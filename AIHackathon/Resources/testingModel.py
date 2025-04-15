import sys
import os
import io

out = sys.stdout
err = sys.stderr

sys.stderr = io.StringIO()
sys.stdout = io.StringIO()

os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'

import json
import argparse
import numpy as np
import pandas as pd
import glob
from tensorflow.keras.models import load_model
from tensorflow.keras.metrics import MeanSquaredError as MSE
from sklearn.metrics import mean_squared_error
import joblib
import xgboost as xgb
import h5py
from sklearn.base import BaseEstimator
from sklearn.metrics import mean_squared_error, mean_absolute_error


# Константы для библиотек
KERAS = "Keras"
SCIKIT_LEARN = "Scikit-learn"
XGBOOST = "XGBoost"
UNKNOWN = "Unknown"

# Константы для ошибок
ERROR_UNKNOWN_FORMAT = "Неизвестный формат модели"
ERROR_INTERNAL = "Внутренняя ошибка: не удалось найти файл с моделью"

# Функция для создания стандартного результата
def create_result(library, mse_value, error=None):
    return {
        "Library": library,
        "Accuracy": mse_value,
        "Error": error
    }

# Загрузка подготовленных данных
def load_prepared_data(image_file, keypoints_file):
    prepared_data = np.load(image_file)
    prepared_keypoints = pd.read_csv(keypoints_file)
    x_test = prepared_data['images']
    y_test = prepared_keypoints.values
    return x_test, y_test

# Функция для вычисления MSE и преобразования в точность
def metric(y_test, y_pred):
    mse = mean_squared_error(y_test, y_pred)
    mae = mean_absolute_error(y_test, y_pred)
    return mse / 100 + mae

# Автоматическое подгонка входных данных под модель
def auto_adjust_input(model, x):
    if hasattr(model, 'input_shape'):
        return x  # Keras ожидает исходную форму входных данных
    if hasattr(model, 'n_features_in_'):
        if x.ndim > 2:
            x = x.reshape((x.shape[0], -1))
        if x.shape[1] != model.n_features_in_:
            raise ValueError(f"Model expects {model.n_features_in_} features, got {x.shape[1]}")
        return x
    return x.reshape((x.shape[0], -1)) if x.ndim > 2 else x

# Определение типа модели по файлу
def identify_model_library(model_file):
    # Проверка для Keras (формат HDF5)
    if model_file.endswith('.h5') or model_file.endswith('.keras'):
        return KERAS
    
    # Проверка для scikit-learn и XGBoost (joblib или pickle)
    try:
        model = joblib.load(model_file)
        if isinstance(model, BaseEstimator):
            return SCIKIT_LEARN
    except Exception:
        pass
    
    # Проверка для XGBoost с расширением .model или .bin
    if model_file.endswith('.model') or model_file.endswith('.xgb') or model_file.endswith('.ubj'):
        return XGBOOST
    
    # Проверка для Keras (формат HDF5)
    try:
        with h5py.File(model_file, 'r') as f:
            return KERAS
    except Exception:
        pass
    
    return UNKNOWN

# Функция для загрузки и тестирования модели
def load_and_evaluate_model(model_file, x_test, y_test):
    model_type = identify_model_library(model_file)
    
    try:
        if model_type == KERAS:
            model = load_model(model_file, custom_objects={'mse': MSE()})
            x_test = auto_adjust_input(model, x_test)
            y_pred = model.predict(x_test)
            m_val = metric(y_test, y_pred)
            return create_result(KERAS, m_val)
        elif model_type == SCIKIT_LEARN:
            model = joblib.load(model_file)
            x_test = auto_adjust_input(model, x_test)
            y_pred = model.predict(x_test)
            m_val = metric(y_test, y_pred)
            return create_result(SCIKIT_LEARN, m_val)
        elif model_type == XGBOOST:
            model = xgb.Booster()
            model.load_model(model_file)
            x_test = auto_adjust_input(model, x_test)
            dtest = xgb.DMatrix(x_test)
            y_pred = model.predict(dtest)
            m_val = metric(y_test, y_pred)
            return create_result(XGBOOST, m_val)
        else:
            return create_result(UNKNOWN, 0, ERROR_UNKNOWN_FORMAT)
    
    except Exception as e:
        return create_result(model_type, 0, f"Ошибка при обработке модели: {str(e)}")

# Основная функция
def main():
    parser = argparse.ArgumentParser(description="Оценка модели на подготовленных данных")
    parser.add_argument('image_file', type=str, help="Путь до .npz файла с изображениями")
    parser.add_argument('keypoints_file', type=str, help="Путь до .csv файла с ключевыми точками")
    args = parser.parse_args()

    model_files = glob.glob("model.*")
    if not model_files:
        print(json.dumps(create_result(UNKNOWN, 0, ERROR_INTERNAL)), file=out)
        return

    model_file = model_files[0]
    x_test, y_test = load_prepared_data(args.image_file, args.keypoints_file)

    result = load_and_evaluate_model(model_file, x_test, y_test)
    
    # Теперь выводим ошибку и привязываем её к соответствующей модели
    if result["Error"] is None:
        print(json.dumps(result), file=out)
    else:
        print(json.dumps(create_result(result["Library"], 0, f"Модель не может быть обработана. Подробности:\n{result['Error']}")), file=out)

if __name__ == "__main__":
    main()
def save_and_split_model(model, output_path, clear_dir=False):
    """
    Сохраняет модель и разбивает её на части с заголовками.

    Поддерживаемые типы моделей:
        - Keras
        - Sklearn
        - XGBoost

    Модель сохраняется во временный файл с расширением, соответствующим её типу:
        - .h5  — для Keras
        - .pkl — для Sklearn
        - .ubj — для XGBoost

    Затем файл разбивается на части по 19 МБ (по умолчанию), 
    каждая часть содержит:
        - SHA-256 хеш всей модели (32 байта)
        - Общее количество частей (1 байт)
        - Индекс текущей части (1 байт)
        - Данные части модели

    После разбиения оригинальный файл модели удаляется.

    Args:
        model: Объект модели, которую необходимо сохранить и разбить.
        output_path (str): Путь до места сохранения, включая имя без расширения 
                           (например, "./Tmp/ResultModel").
        clear_dir (bool, optional): Очистить ли директорию перед сохранением. 
                                    По умолчанию False.

    Raises:
        TypeError: Если тип модели не распознан.
        ValueError: Если количество частей превышает 255.
    """
    import os
    import shutil
    import hashlib
    def get_model_type(model):
        try:
            import keras
            if isinstance(model, keras.Model):
                return 'keras'
        except ImportError:
            pass
        try:
            import xgboost
            if isinstance(model, xgboost.Booster) or isinstance(model, xgboost.XGBModel):
                return 'xgboost'
        except ImportError:
            pass
        try:
            from sklearn.base import BaseEstimator
            if isinstance(model, BaseEstimator):
                return 'sklearn'
        except ImportError:
            pass
        raise TypeError("Неизвестный тип модели")
    def save_model(model, path, model_type):
        if model_type == 'keras':
            from keras.models import save_model
            save_model(model, path)
        elif model_type == 'sklearn':
            import joblib
            joblib.dump(model, path)
        elif model_type == 'xgboost':
            model.save_model(path)
    def split_file_with_hash_and_index(input_path, output_dir, name, ext, max_part_size=19 * 1024 * 1024):
        with open(input_path, 'rb') as f:
            data = f.read()
        total_size = len(data)
        file_hash = hashlib.sha256(data).digest()
        header_size = 32 + 1 + 1
        chunk_size = max_part_size - header_size
        total_parts = (total_size + chunk_size - 1)
        if total_parts > 255:
            raise ValueError("Слишком много частей (> 255).")
        os.makedirs(output_dir, exist_ok=True)
        for index in range(total_parts):
            part_data = data[index * chunk_size : (index + 1) * chunk_size]
            part_filename = os.path.join(output_dir, f'{name}_part_{index:03d}{ext}')
            with open(part_filename, 'wb') as part_file:
                part_file.write(file_hash)
                part_file.write(bytes([total_parts]))
                part_file.write(bytes([index]))
                part_file.write(part_data)
    model_type = get_model_type(model)
    base_path_no_ext = os.path.splitext(output_path)[0]
    if model_type == 'keras':
        output_path = base_path_no_ext + ".h5"
    elif model_type == 'sklearn':
        output_path = base_path_no_ext + ".pkl"
    elif model_type == 'xgboost':
        output_path = base_path_no_ext + ".ubj"
    output_dir = os.path.dirname(output_path)
    name, ext = os.path.splitext(os.path.basename(output_path))
    if clear_dir and os.path.exists(output_dir):
        shutil.rmtree(output_dir)
    os.makedirs(output_dir, exist_ok=True)
    save_model(model, output_path, model_type)
    split_file_with_hash_and_index(output_path, output_dir, name, ext)
    if os.path.exists(output_path):
        os.remove(output_path)
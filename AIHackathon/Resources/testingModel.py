import importlib.util
import sys
import numpy as np
import pandas as pd
import argparse
from sklearn.metrics import mean_squared_error, mean_absolute_error
from contextlib import contextmanager
import os
from predict_fn import predict

def load_function_from_file(file_path, function_name):
    module_name = file_path.replace('/', '.').rstrip('.py')
    spec = importlib.util.spec_from_file_location(module_name, file_path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[module_name] = module
    spec.loader.exec_module(module)
    return getattr(module, function_name)

def main():
    try:
        parser = argparse.ArgumentParser(description="Оценка модели на подготовленных данных")
        parser.add_argument('image_file', type=str, help="Путь до .npz файла с изображениями")
        args = parser.parse_args()
        prepared_data = np.load(args.image_file)['images']
        all_preds = []
        batch_size = 32
        num_batches = len(prepared_data) // batch_size + (1 if len(prepared_data) % batch_size != 0 else 0)
        for i in range(num_batches):
            batch_data = prepared_data[i * batch_size: (i + 1) * batch_size]
            y_pred_batch = predict(batch_data)
            all_preds.append(y_pred_batch)
        y_pred = np.concatenate(all_preds, axis=0)
        with open('output.txt', 'wb') as f:
            np.save(f, y_pred)

    except Exception as e:
        print({
            "Error": str(e)
        }, flush=True)

if __name__ == "__main__":
    main()
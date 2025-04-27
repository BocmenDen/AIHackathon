import numpy as np
import pandas as pd
from sklearn.metrics import mean_squared_error, mean_absolute_error
import argparse

def evaluate(y_true_csv_path: str, y_pred_txt_path: str):
    try:
        y_true = pd.read_csv(y_true_csv_path).values
        with open(y_pred_txt_path, 'rb') as f:
            y_pred = np.load(f)
        if y_true.shape != y_pred.shape:
            raise ValueError(f"Shape mismatch: y_true {y_true.shape} vs y_pred {y_pred.shape}")
        mse = mean_squared_error(y_true, y_pred)
        mae = mean_absolute_error(y_true, y_pred)
        final_score = (mse / 100) + mae
        print({
            "Accuracy": float(final_score),
            "Error": ""
        })
    except Exception as e:
        print({
            "Accuracy": 100000000,
            "Error": str(e)
        })

def main():
    parser = argparse.ArgumentParser(description="Evaluate model predictions")
    parser.add_argument('--true', type=str, required=True, help='Путь к CSV-файлу с правильными результатами')
    parser.add_argument('--pred', type=str, required=True, help='Путь к TXT-файлу с предсказанными результатами')
    args = parser.parse_args()

    evaluate(args.true, args.pred)

if __name__ == "__main__":
    main()
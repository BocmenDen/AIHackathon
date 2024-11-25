#python -m pip install tensorflow
#python -m pip install scikit-learn
#python -m pip install xgboost
#python -m pip install pandas

import tensorflow as tf
import xgboost as xgb
import pandas as pd
from sklearn.metrics import accuracy_score
import sys
import io

def getPredict(pathModel):
  try:
    model = tf.keras.models.load_model(pathModel)
    fPredict = lambda x: model.predict(x)
    nameLibrary = "tensorflow"
    return (nameLibrary, fPredict)
  except Exception:
    pass
  try:
    model = xgb.Booster()
    model.load_model(pathModel)
    fPredict = lambda x: model.predict(xgb.DMatrix(data=x), output_margin=False)
    nameLibrary = "xgboost"
    return (nameLibrary, fPredict)
  except Exception:
    pass

def getAccuracyScore(modelPath, pathTestDB):
  (libraryName, fPredict) = getPredict(modelPath)
  if libraryName == None:
    raise "Не удалось определить тип модели"
  data = pd.read_csv(pathTestDB)
  x_test = data.drop('Winning_Chanse', axis=1)
  y_test = data['Winning_Chanse']
  y_pred = fPredict(x_test) >= 0.5
  accuracy =  accuracy_score(y_test, y_pred)
  return (libraryName, accuracy)

def main():
    # Bug-TODO один из модулей во время прогнозирования данных в консоль печатает недопустимые символы
    # из-за чего всё КРАШИТЬСЯ при запуске только из C# (что-то не то с кодировкой вывода в stderr/stdout)
    # https://github.com/keras-team/keras/issues/19386
    out = sys.stdout
    err = sys.stderr
    sys.stderr = io.StringIO()
    sys.stdout = io.StringIO()
    data = getAccuracyScore(sys.argv[1], sys.argv[2])
    print(f"{data[0]}<split>{data[1]:.6f}", file=out)

if __name__ == "__main__":
  sys.exit(main())
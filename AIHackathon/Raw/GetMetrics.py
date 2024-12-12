#python -m pip install tensorflow
#python -m pip install scikit-learn
#python -m pip install xgboost
#python -m pip install pandas

import io
import sys
import json
import tensorflow as tf
import xgboost as xgb
import pandas as pd
from sklearn.metrics import accuracy_score, roc_auc_score
from itertools import islice

def getPredict(pathModel):
  try:
    model = tf.keras.models.load_model(pathModel)
    fPredict = lambda x: model.predict(x)
    nameLibrary = "keras"
    return (nameLibrary, fPredict, None)
  except Exception:
    pass
  try:
    model = xgb.Booster()
    model.load_model(pathModel)
    fPredict = lambda x: model.predict(xgb.DMatrix(data=x), output_margin=False)
    nameLibrary = "xgboost"
    return (nameLibrary, fPredict, model.feature_names)
  except Exception:
    pass
  raise Exception("Ну удалось распознать тип библиотеки")

def getDB(pathFile, tergetColumn, coulumns=None):
  data = pd.read_csv(pathTestDB)
  y_test = data[targetColumn]
  x_test = data.drop(targetColumn, axis=1)
  if(columns != None and len(columns)!=0):
    x_test = x_test[coulumns]
  return (x_test, y_test)

out = sys.stdout
err = sys.stderr

sys.stderr = io.StringIO()
sys.stdout = io.StringIO()

try:
    (modelPath, pathTestDB, targetColumn) = (sys.argv[1], sys.argv[2], sys.argv[3])

    (libraryName, fPredict, columns) = getPredict(modelPath)
    columns = columns or list(islice(sys.argv, 4, None)) if len(sys.argv) > 3 else columns
    (x_test, y_true) = getDB(pathTestDB, targetColumn, columns)
    y_pred = fPredict(x_test) >= 0.5

    accuracy = accuracy_score(y_true, y_pred)
    roc_auc = roc_auc_score(y_true, y_pred)

    print(json.dumps({
        'library': libraryName,
        'accuracy': accuracy,
        'roc_auc': roc_auc,
        'columns': "default" if columns == None or len(columns) == 0 else ", ".join(columns)
    }), file=out)
except Exception as e:
    print(json.dumps({
        'error': str(e)
    }), file=out)
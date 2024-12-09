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
  raise Exception("Ну удалось распознать тип библиотеки")

out = sys.stdout
err = sys.stderr

sys.stderr = io.StringIO()
sys.stdout = io.StringIO()

try:
    (modelPath, pathTestDB, targetColumn) = (sys.argv[1], sys.argv[2], sys.argv[3])
    (libraryName, fPredict) = getPredict(modelPath)

    data = pd.read_csv(pathTestDB)
    x_test = data.drop(targetColumn, axis=1)
    y_true = data[targetColumn]
    y_pred = fPredict(x_test) >= 0.5

    accuracy = accuracy_score(y_true, y_pred)
    roc_auc = roc_auc_score(y_true, y_pred)

    print(json.dumps({
        'library': libraryName,
        'accuracy': accuracy,
        'roc_auc': roc_auc
    }), file=out)
except Exception as e:
    print(json.dumps({
        'error': str(e.error)
    }), file=out)
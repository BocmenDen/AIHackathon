*__Несколько примеров для сохранения перед отправкой__*

*__Keras__*
```python
# 1. Создание и обучение модели
model = ...
....
# 2. Далее сохраняете модель к примеру в файл "keras.h5"
model.save('keras.h5')

# 3. Сформируйте функцию которая загрузит модель из файла и сформирует предсказания из входных данных
def predict(input_data, path_to_model):
    from tensorflow.keras.models import load_model
    from tensorflow.keras.metrics import MeanSquaredError as MSE
    model = load_model(path_to_model, custom_objects={'mse': MSE()})
    return model.predict(input_data)

# 4. Сохраняете файлы для отправки боту к примеру в папку "FolderSave"
package_model('./FolderSave', './keras.h5', './TraningModel.py', predict)
```
*__XgBoost__*
```python
# 1. Создание и обучение модели
model = ...
....
# 2. Далее сохраняете модель к примеру в файл 'trained_model.pkl'
model.save_model("xgbooost.model")

# 3. Сформируйте функцию которая загрузит модель из файла и сформирует предсказания из входных данных
def predict(input_data, path_to_model):
    import xgboost as xgb
    model = xgb.Booster()
    model.load_model(path_to_model)
    # Например, для преобразования [N, W, H, 1] в [N, W * H]
    input_data = input_data.reshape((input_data.shape[0], -1))
    return model.predict(input_data)

# 4. Сохраняете файлы для отправки боту к примеру в папку "FolderSave", True - очистит папку перед сохранением
package_model('./FolderSave', './xgbooost.model', './TraningModel.py', predict, True)
```
*__Scikit-Learn__*
```python
# 1. Создание и обучение модели
model = ...
....
# 2. Далее сохраняете модель к примеру в файл "sklearn.pkl"
import joblib
joblib.dump(model, 'sklearn.pkl')

# 4. Сформируйте функцию которая загрузит модель из файла и сформирует предсказания из входных данных
def predict(input_data, path_to_model):
    import joblib
    model = joblib.load(path_to_model)
    # Например, для преобразования [N, W, H, C] в [N, W * H]
    input_data = input_data.reshape((input_data.shape[0], -1))
    return model.predict(input_data)

# 4. Сохраняете файлы для отправки боту к примеру в папку "FolderSave", True - очистит папку перед сохранением
package_model('./FolderSave', './sklearn.pkl', './TraningModel.py', predict, True)
```
*__PyTorch__*
```python
# ========== PyTorch

# 1. Опишите класс модели в отдельном файле (например PyTorchModel.py)
import torch.nn as nn
    
class MyModel(nn.Module):
    def __init__(self):
        super().__init__()
        self.net = nn.Sequential(...)
    def forward(self, x):
        return self.net(x)

# 2. Импортируйте вашу модель из файла
from PyTorchModel import MyModel

model = MyModel()

# 3. Обучите вашу модель
....

# 4. Сформируйте функцию которая загрузит модель из файла и сформирует предсказания из входных данных
def predict(input_data, path_to_model):
    import torch
    import torch.nn as nn
    from PyTorchModel import MyModel
    model = MyModel()
    model.load_state_dict(torch.load(path_to_model))
    model.eval()
    with torch.no_grad():
        # На входе к примеру [N, W, H, C], а PyTorch нужен порядок [N, C, H, W] 
        input_data = torch.from_numpy(input_data).permute(0, 3, 1, 2).float()
        return model(input_data).numpy()

# 5. Далее сохраняете модель к примеру в файл "pytorch.pt"
import torch
torch.save(model.state_dict(), "pytorch.pt")

# 6. Сохраняете файлы для отправки боту к примеру в папку "FolderSave", True - очистит папку перед сохранением
# ["./PyTorchModel.py"] - путь к файлу с классом модели, будет внедрён как дополнительный ресурс
package_model('./ResPyTorch', "pytorch.pt", "./del.py", predict, True, ["./PyTorchModel.py"])
```
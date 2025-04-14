# Используем официальный образ Python
FROM python:3.11-slim

# Устанавливаем необходимые зависимости
RUN apt-get update && apt-get install -y \
    build-essential \
    python3-dev \
    python3-pip \
    && rm -rf /var/lib/apt/lists/*

# Устанавливаем необходимые Python-библиотеки
RUN pip install --upgrade pip && \
    pip install \
    numpy==2.0.2 \
    pandas==2.2.3 \
    tensorflow==2.18.0 \
    scikit-learn==1.5.2 \
    xgboost==2.1.2

# Рабочая директория в контейнере
WORKDIR /app

# Копируем все необходимые файлы (например, скрипты, модель, и т.д.) в контейнер
COPY . /app

# Точка входа для выполнения вашего скрипта
CMD ["python", "testingModel.py", "prepared_images.npz", "prepared_keypoints.csv"]
FROM python:3.11-slim

RUN apt-get update && apt-get install -y \
    build-essential \
    python3-dev \
    python3-pip \
    && rm -rf /var/lib/apt/lists/*

RUN pip install --upgrade pip && \
    pip install \
    numpy==2.0.2 \
    pandas==2.2.3 \
    tensorflow==2.18.0 \
    scikit-learn==1.5.2 \
    xgboost==2.1.2 \ 
    torch==2.6.0 \
    joblib==1.4.2 \ 
    torchvision \ 
    opencv-python

WORKDIR /app

COPY . /app
 
CMD ["python", "testingModel.py", "prepared_images.npz"]
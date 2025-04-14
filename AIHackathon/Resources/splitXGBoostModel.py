def save_and_split_xgboost_model(model, output_path, clear_dir=False):
    """
    Сохраняет модель XGBoost и разбивает на части.

    Args:
        model: Объект модели XGBoost.
        output_path (str): Путь до места сохранения, включая имя без расширения.
        clear_dir (bool, optional): Очистить ли директорию перед сохранением. 
                                    По умолчанию False.
    """
    import os
    import shutil
    import hashlib

    def _split_file_with_hash(input_path, output_dir, name, ext, max_part_size=19 * 1024 * 1024):
        with open(input_path, 'rb') as f:
            data = f.read()

        file_hash = hashlib.sha256(data).digest()
        header_size = 32 + 1 + 1
        chunk_size = max_part_size - header_size
        total_parts = (len(data) + chunk_size - 1) // chunk_size

        if total_parts > 255:
            raise ValueError("Слишком много частей (> 255)")

        for index in range(total_parts):
            part_data = data[index * chunk_size : (index + 1) * chunk_size]
            part_filename = os.path.join(output_dir, f'{name}_part_{index:03d}{ext}')
            with open(part_filename, 'wb') as part_file:
                part_file.write(file_hash)
                part_file.write(bytes([total_parts]))
                part_file.write(bytes([index]))
                part_file.write(part_data)

    # Определяем путь к файлу с добавлением расширения
    base_path_no_ext = os.path.splitext(output_path)[0]
    output_path = base_path_no_ext + ".ubj"

    output_dir = os.path.dirname(output_path)
    name, ext = os.path.splitext(os.path.basename(output_path))

    # Очищаем директорию, если указано
    if clear_dir and os.path.exists(output_dir):
        shutil.rmtree(output_dir)
    os.makedirs(output_dir, exist_ok=True)

    # Сохраняем модель XGBoost в файл
    model.save_model(output_path)

    # Разбиваем на части
    _split_file_with_hash(output_path, output_dir, name, ext)

    # Удаляем оригинальный файл
    if os.path.exists(output_path):
        os.remove(output_path)
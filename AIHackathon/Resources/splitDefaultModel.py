def split_file_with_hash_and_index(input_path, output_dir):
    """
    Разбивает модель на части, добавляя в каждую часть заголовок с хешем, 
    количеством частей и индексом текущей части.

    Каждая часть содержит:
        - SHA-256 хеш всего файла (32 байта)
        - Общее количество частей (1 байт)
        - Индекс текущей части (1 байт)
        - Данные фрагмента

    Полезно для отправки больших файлов по частям, например, через Telegram-ботов.

    Args:
        input_path (str): Путь к исходному файлу, который нужно разбить.
        output_dir (str): Путь к директории, в которую будут сохранены части.

    Raises:
        ValueError: Если количество частей превышает 255 (ограничение по заголовку).
    """
    import os
    import hashlib
    max_part_size=19 * 1024 * 1024
    with open(input_path, 'rb') as f:
        data = f.read()

    total_size = len(data)
    file_hash = hashlib.sha256(data).digest()
    header_size = 32 + 1 + 1
    chunk_size = max_part_size - header_size

    total_parts = (total_size + chunk_size - 1) // chunk_size
    if total_parts > 255:
        raise ValueError("Слишком много частей (> 255). Уменьшите размер файла или увеличьте размер части.")

    name, ext = os.path.splitext(os.path.basename(input_path))

    os.makedirs(output_dir, exist_ok=True)

    for index in range(total_parts):
        part_data = data[index * chunk_size : (index + 1) * chunk_size]
        part_filename = os.path.join(output_dir, f'{name}_part_{index:03d}{ext}')

        with open(part_filename, 'wb') as part_file:
            part_file.write(file_hash)            # 32 байта — SHA-256
            part_file.write(bytes([total_parts])) # 1 байт — количество частей
            part_file.write(bytes([index]))       # 1 байт — индекс текущей части
            part_file.write(part_data)

        print(f"Создан файл: {part_filename} (часть {index+1}/{total_parts}, {len(part_data)} байт данных)")

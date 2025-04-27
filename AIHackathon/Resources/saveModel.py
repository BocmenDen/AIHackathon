def package_model(output_dir: str,  model_path: str, source_code_path: str, prediction_fn: callable, isClearDirectory: bool = False, extra_files: list[str] = None):
    """
    Упаковывает модель, функцию предсказания, исходный файл и дополнительные файлы в один архив,
    а затем разбивает его на части с метаданными.
    Args:
        output_dir (str): Папка для результата
        model_path (str): Путь к файлу модели
        source_code_path (str): Путь к исходному файлу
        prediction_fn (callable): Функция предсказания
        isClearDirectory (bool): Очистить директорию перед сохранением
        extra_files (list[str]): Список дополнительных файлов для внедрения
    """
    import os
    import shutil
    import tempfile
    import inspect
    import ast
    import astor
    from datetime import datetime
    import hashlib
    max_part_size = 19 * 1024 * 1024
    header_size = 32 + 1 + 1
    chunk_size = max_part_size - header_size
    def rename_top_level_function(source_code, new_name):
        tree = ast.parse(source_code)
        for node in tree.body:
            if isinstance(node, ast.FunctionDef):
                node.name = new_name
                if len(node.args.args) == 2:
                    node.args.defaults = [ast.Constant(value=None), ast.Constant(value=model_path)]
                break
        return astor.to_source(tree)
    if isClearDirectory and os.path.exists(output_dir):
        shutil.rmtree(output_dir)
    os.makedirs(output_dir, exist_ok=True)
    with tempfile.TemporaryDirectory() as temp_dir:
        model_name = os.path.basename(model_path)
        model_copy_path = os.path.join(temp_dir, model_name)
        shutil.copy2(model_path, model_copy_path)
        predict_code_path = os.path.join(temp_dir, "predict_fn.py")
        original_source = inspect.getsource(prediction_fn)
        normalized_code = rename_top_level_function(original_source, "predict")
        with open(predict_code_path, "w", encoding="utf-8") as f:
            f.write(normalized_code)
        target_filename = f"bot_package_{datetime.now().strftime('%Y%m%d_%H%M%S')}.bpk"
        target_path = os.path.join(output_dir, target_filename)
        files_to_add = [
            {"name": model_name, "path": model_copy_path},
            {"name": "predict_fn.py", "path": predict_code_path},
            {"name": f"source_code{os.path.splitext(source_code_path)[1]}", "path": source_code_path}
        ]
        if extra_files:
            for path in extra_files:
                files_to_add.append({
                    "name": os.path.basename(path),
                    "path": path
                })
        with open(target_path, "wb") as target_file:
            for file_info in files_to_add:
                target_file.write(f"{file_info['name']}\n".encode('utf-8'))
                with open(file_info["path"], "rb") as f:
                    file_data = f.read()
                start = target_file.tell()
                end = start + len(file_data)
                target_file.write(f"{start}-{end}\n".encode('utf-8'))
                target_file.write(file_data)
        with open(target_path, 'rb') as f:
            data = f.read()
        total_size = len(data)
        file_hash = hashlib.sha256(data).digest()
        total_parts = (total_size + chunk_size - 1) // chunk_size
        base_name, ext = os.path.splitext(os.path.basename(target_path))
        for index in range(total_parts):
            part_data = data[index * chunk_size : (index + 1) * chunk_size]
            part_filename = os.path.join(output_dir, f'{base_name}_part_{index:03d}{ext}')
            with open(part_filename, 'wb') as part_file:
                part_file.write(file_hash)
                part_file.write(bytes([total_parts]))
                part_file.write(bytes([index]))
                part_file.write(part_data)
            print(f"[📦] Часть {index+1}/{total_parts} сохранена: {part_filename} ({len(part_data)} байт)")
        os.remove(target_path)
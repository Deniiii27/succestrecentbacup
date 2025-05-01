# PythonEngine/output_writer.py
def save_output(text, output_path):
    with open(output_path, "w", encoding="utf-8") as f:
        f.write(text)

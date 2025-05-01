# PythonEngine/file_parser.py
import pandas as pd
from docx import Document
import os

def extract_content(file_path):
    ext = os.path.splitext(file_path)[1].lower()

    if ext == ".xlsx" or ext == ".xls":
        df = pd.read_excel(file_path)
        return df.to_string(index=False)

    elif ext == ".csv":
        df = pd.read_csv(file_path)
        return df.to_string(index=False)

    elif ext == ".docx":
        doc = Document(file_path)
        return "\n".join([p.text for p in doc.paragraphs])

    else:
        return "Unsupported file type."


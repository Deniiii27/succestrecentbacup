# PythonEngine/file_parser.py
import pandas as pd
from docx import Document
import os

def extract_smart_snippet(text_list, parts=3):
    if not text_list:
        return ""

    chunk_size = max(len(text_list) // parts, 1)
    snippet = []

    for i in range(parts):
        idx = min(i * chunk_size, len(text_list) - 1)
        snippet.append(text_list[idx])

    return "\n".join(snippet)

def extract_content(file_path):
    ext = os.path.splitext(file_path)[1].lower()

    try:
        if ext in [".xlsx", ".xls"]:
            df = pd.read_excel(file_path)
        elif ext == ".csv":
            df = pd.read_csv(file_path)
        elif ext == ".docx":
            doc = Document(file_path)
            paragraphs = [p.text.strip() for p in doc.paragraphs if p.text.strip()]
            third = len(paragraphs) // 3
            snippet = (
                paragraphs[:3] +
                paragraphs[third:third+3] +
                paragraphs[-3:]
            )
            return "\n".join(snippet)
        else:
            return "Unsupported file type."

        # Untuk tabular file (csv, excel): ambil 10 baris awal, tengah, akhir
        num_rows = len(df)
        third = num_rows // 3
        snippet_df = pd.concat([
            df.head(10),
            df.iloc[third:third+10],
            df.tail(10)
        ])
        return snippet_df.to_string(index=False)

    except Exception as e:
        return f"Gagal membaca file: {e}"

# PythonEngine/analysis_prompt.py
from api_client import call_gemini

def enhance_prompt(user_prompt):
    prompt_template = f"""
Kamu bertugas memperbaiki file data berikut sesuai instruksi dari user.

Hasil akhir WAJIB ditampilkan dalam format tabel Markdown seperti contoh ini:

| Kolom1 | Kolom2 |
|--------|--------|
| Nilai1 | Nilai2 |
| Nilai3 | Nilai4 |

?? Jangan menambahkan penjelasan apa pun di luar tabel. Tampilkan HANYA tabel yang sudah diperbaiki.

Instruksi user: {user_prompt.strip()}
"""
    return prompt_template

def run_analysis(file_text, user_prompt):
    prompt = enhance_prompt(user_prompt)
    combined_prompt = f"{prompt}\n\nBerikut isi dokumen (cuplikan data):\n{file_text}"
    return call_gemini(combined_prompt)

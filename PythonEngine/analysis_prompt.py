# PythonEngine/analysis_prompt.py
from api_client import call_gemini  # atau call_deepseek

def run_analysis(file_text, user_prompt):
    prompt = f"{user_prompt}\n\nBerikut isi dokumen:\n{file_text[:3000]}"  # Batasin teks agar tidak kepanjangan
    return call_gemini(prompt)  # atau call_deepseek(prompt)

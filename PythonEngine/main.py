# PythonEngine/main.py
import sys
from file_parser import extract_content
from analysis_prompt import run_analysis
from output_writer import save_output
import pandas as pd
import re

def parse_and_export_excel(output_txt_path: str, output_excel_path: str):
    with open(output_txt_path, "r", encoding="utf-8") as file:
        content = file.read()

    # Cari bagian "Masalah:" dan "Rekomendasi:" dengan regex
    pattern = r"Masalah:\s*(.*?)\s*Rekomendasi:\s*(.*?)(?:\n\n|\Z)"
    matches = re.findall(pattern, content, re.DOTALL)

    data = [{"Masalah": m[0].strip(), "Rekomendasi": m[1].strip()} for m in matches]

    # Simpan ke file Excel
    df = pd.DataFrame(data)
    df.to_excel(output_excel_path, index=False)
    print(f"Excel berhasil disimpan ke {output_excel_path}")

def main():
    if len(sys.argv) < 4:
        print("Usage: python main.py <file_path> <output_txt_path> <user_prompt>")
        return

    file_path = sys.argv[1]
    output_txt_path = sys.argv[2]
    user_prompt = sys.argv[3]

    # 1. Ambil isi file
    content = extract_content(file_path)

    # 2. Proses dengan AI
    result_text = run_analysis(content, user_prompt)

    # 3. Simpan output hasil AI ke TXT
    save_output(result_text, output_txt_path)

    # 4. Parse hasil AI dan simpan ke Excel
    output_excel_path = output_txt_path.replace(".txt", "_parsed.xlsx")
    parse_and_export_excel(output_txt_path, output_excel_path)

    print("OK")  # Sinyal ke C# bahwa proses selesai

if __name__ == "__main__":
    main()

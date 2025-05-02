# PythonEngine/main.py
import sys
from file_parser import extract_content
from analysis_prompt import run_analysis
from output_writer import save_output
import pandas as pd

def parse_and_export_excel(output_txt_path: str, output_excel_path: str):
    from io import StringIO

    with open(output_txt_path, "r", encoding="utf-8") as file:
        content = file.read()

    # Filter baris yang mengandung tabel markdown (tanda "|")
    lines = [line for line in content.splitlines() if '|' in line]
    if len(lines) < 2:
        print("[GAGAL] Tidak cukup data tabel.")
        return

    headers = [h.strip() for h in lines[0].strip('|').split('|')]
    rows = [line.strip('|').split('|') for line in lines[2:]]  # Skip garis pemisah

    data = [[cell.strip() for cell in row] for row in rows]
    df = pd.DataFrame(data, columns=headers)
    df.to_excel(output_excel_path, index=False)
    print(f"[SUKSES] File Excel hasil perbaikan disimpan ke {output_excel_path}")

def main():
    if len(sys.argv) < 4:
        print("Usage: python main.py <file_path> <output_txt_path> <user_prompt>")
        return

    file_path = sys.argv[1]
    output_txt_path = sys.argv[2]
    user_prompt = sys.argv[3]

    content = extract_content(file_path)
    result_text = run_analysis(content, user_prompt)
    save_output(result_text, output_txt_path)

    output_excel_path = output_txt_path.replace(".txt", "_parsed.xlsx")
    parse_and_export_excel(output_txt_path, output_excel_path)

    print("OK")

if __name__ == "__main__":
    main()

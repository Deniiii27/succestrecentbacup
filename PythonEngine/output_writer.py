# PythonEngine/output_writer.py

import pandas as pd

def save_output(text_result, output_txt_path):
    # Simpan hasil teks ke file txt
    with open(output_txt_path, 'w', encoding='utf-8') as f:
        f.write(text_result)

    # Buat file Excel hasil parsing jika format cocok
    parsed_rows = []
    current = {}

    for line in text_result.splitlines():
        line = line.strip()
        if line.startswith("Masalah:"):
            current['Masalah'] = line.replace("Masalah:", "").strip()
        elif line.startswith("Rekomendasi:"):
            current['Rekomendasi'] = line.replace("Rekomendasi:", "").strip()
        elif line.startswith("Catatan:"):
            current['Catatan'] = line.replace("Catatan:", "").strip()
            parsed_rows.append(current)
            current = {}

    if parsed_rows:
        df = pd.DataFrame(parsed_rows)
        output_excel_path = output_txt_path.replace("hasil_output.txt", "hasil_parsed.xlsx")
        df.to_excel(output_excel_path, index=False)

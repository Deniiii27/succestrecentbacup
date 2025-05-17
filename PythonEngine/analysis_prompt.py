# PythonEngine/analysis_prompt.py - Update

from api_client import call_gemini

def enhance_prompt(user_prompt, output_format: str):
    if output_format == "excel":
        prompt_template = f"""
Kamu bertugas memperbaiki atau menyusun ulang data berikut berdasarkan instruksi user.

Hasil akhir WAJIB ditampilkan dalam format tabel Markdown seperti ini:

| Kolom1 | Kolom2 |
|--------|--------|
| Nilai1 | Nilai2 |
| Nilai3 | Nilai4 |

INSTRUKSI PENTING UNTUK EXCEL:
1. Jika ada perhitungan matematika atau formula yang perlu ditambahkan, gunakan format rumus Excel sebagai berikut: =SUM(A1:A5), =AVERAGE(B1:B10), =A1+B1, dst.
2. Pastikan referensi sel (contoh: A1, B2) ditulis dengan benar dan sesuai dengan posisi data pada tabel.
3. Jangan menuliskan "Formula:" atau teks lain sebelum rumus, langsung tulis =RUMUS saja.
4. Semua rumus Excel harus diawali dengan tanda '=' (sama dengan).

?? Jangan menambahkan penjelasan di luar tabel. Hanya tampilkan tabel Markdown sesuai hasil akhir.

Instruksi user: {user_prompt.strip()}
"""
    elif output_format == "word":
        prompt_template = f"""
Kamu bertugas menyusun teks berdasarkan instruksi user.

FORMAT OUTPUT:
1. Gunakan format Markdown untuk struktur dokumen:
   - # untuk judul utama
   - ## untuk sub judul
   - ### untuk sub-sub judul
   - Gunakan baris kosong untuk memisahkan paragraf
   - Gunakan * atau - di awal baris untuk bullet points
   - Untuk penomoran, gunakan 1. 2. 3. dan seterusnya

2. Jika diminta membuat biografi atau profil:
   - Gunakan format "Bidang: Nilai" untuk informasi seperti "Nama:", "Tanggal Lahir:", dll.
   - Buat paragraf terpisah untuk latar belakang, pendidikan, karir, dll.
   - Jika ada, gunakan tabel markdown untuk data terstruktur

3. Untuk dokumen formal:
   - Buat judul yang jelas dan deskriptif
   - Bagi konten menjadi seksi-seksi dengan heading yang jelas
   - Gunakan paragraf yang terstruktur dengan baik

Instruksi user: {user_prompt.strip()}
"""
    else:
        prompt_template = f"""
Kamu bertugas menyusun teks berdasarkan instruksi user.

Silakan berikan hasil akhir dalam format teks biasa, boleh berupa paragraf, heading, bullet, atau bentuk lain yang sesuai konteks.

Instruksi user: {user_prompt.strip()}
"""
    return prompt_template

def run_analysis(file_text: str, user_prompt: str, output_format: str):
    prompt = enhance_prompt(user_prompt, output_format)
    combined_prompt = f"{prompt}\n\nBerikut isi dokumen (cuplikan data):\n{file_text}"
    return call_gemini(combined_prompt)
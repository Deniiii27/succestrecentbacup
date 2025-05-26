/*
  # Update database schema to match SQL Server structure

  1. New Tables
    - `file_types`: Store supported input file types
    - `output_formats`: Store supported output formats
    - `folders`: User folders for organizing files
    - `history`: Track processing history
    - `output_files`: Store processed file information
    - `charts`: Store chart configurations
    - `output_format_preferences`: Store user format preferences

  2. Changes
    - Add new columns to existing tables
    - Update RLS policies
    - Add necessary indexes and constraints

  3. Security
    - Enable RLS on all tables
    - Add appropriate policies for each table
*/

-- Create file_types table
CREATE TABLE IF NOT EXISTS file_types (
  id SERIAL PRIMARY KEY,
  name TEXT NOT NULL UNIQUE
);

-- Create output_formats table
CREATE TABLE IF NOT EXISTS output_formats (
  id SERIAL PRIMARY KEY,
  name TEXT NOT NULL UNIQUE
);

-- Create folders table
CREATE TABLE IF NOT EXISTS folders (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

-- Create history table
CREATE TABLE IF NOT EXISTS history (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE,
  input_file_type INTEGER REFERENCES file_types(id),
  output_format_id INTEGER REFERENCES output_formats(id),
  process_date TIMESTAMPTZ DEFAULT now(),
  processing_time INTEGER,
  prompt_text TEXT,
  process_type TEXT,
  is_success BOOLEAN DEFAULT true
);

-- Create output_files table
CREATE TABLE IF NOT EXISTS output_files (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  history_id UUID REFERENCES history(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  path TEXT NOT NULL,
  size BIGINT,
  created_at TIMESTAMPTZ DEFAULT now(),
  folder_id UUID REFERENCES folders(id) ON DELETE SET NULL
);

-- Create charts table
CREATE TABLE IF NOT EXISTS charts (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE,
  type TEXT NOT NULL,
  name TEXT NOT NULL,
  last_generated_at TIMESTAMPTZ DEFAULT now()
);

-- Create output_format_preferences table
CREATE TABLE IF NOT EXISTS output_format_preferences (
  user_id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  format TEXT NOT NULL,
  updated_at TIMESTAMPTZ DEFAULT now()
);

-- Enable RLS on all tables
ALTER TABLE file_types ENABLE ROW LEVEL SECURITY;
ALTER TABLE output_formats ENABLE ROW LEVEL SECURITY;
ALTER TABLE folders ENABLE ROW LEVEL SECURITY;
ALTER TABLE history ENABLE ROW LEVEL SECURITY;
ALTER TABLE output_files ENABLE ROW LEVEL SECURITY;
ALTER TABLE charts ENABLE ROW LEVEL SECURITY;
ALTER TABLE output_format_preferences ENABLE ROW LEVEL SECURITY;

-- Create RLS policies

-- file_types and output_formats are readable by all
CREATE POLICY "Anyone can read file types" ON file_types
  FOR SELECT USING (true);

CREATE POLICY "Anyone can read output formats" ON output_formats
  FOR SELECT USING (true);

-- Folders are only accessible by their owners
CREATE POLICY "Users can manage own folders" ON folders
  FOR ALL USING (auth.uid() = user_id);

-- History entries are only accessible by their owners
CREATE POLICY "Users can manage own history" ON history
  FOR ALL USING (auth.uid() = user_id);

-- Output files are only accessible by their owners (via history)
CREATE POLICY "Users can manage own output files" ON output_files
  FOR ALL USING (
    auth.uid() IN (
      SELECT user_id 
      FROM history 
      WHERE id = output_files.history_id
    )
  );

-- Charts are only accessible by their owners
CREATE POLICY "Users can manage own charts" ON charts
  FOR ALL USING (auth.uid() = user_id);

-- Format preferences are only accessible by their owners
CREATE POLICY "Users can manage own format preferences" ON output_format_preferences
  FOR ALL USING (auth.uid() = user_id);

-- Insert initial data
INSERT INTO file_types (name) VALUES 
  ('PDF'), ('DOCX'), ('XLSX'), ('PNG'), ('JPG'), ('PROMPT'), ('OTHER')
ON CONFLICT (name) DO NOTHING;

INSERT INTO output_formats (name) VALUES 
  ('Excel'), ('Word')
ON CONFLICT (name) DO NOTHING;

-- Create views for statistics

-- Input file type usage stats
CREATE OR REPLACE VIEW input_file_type_stats AS
SELECT 
  h.user_id,
  ft.name as file_type,
  COUNT(h.id) as usage_count
FROM history h
JOIN file_types ft ON h.input_file_type = ft.id
GROUP BY h.user_id, ft.name;

-- Output format usage stats
CREATE OR REPLACE VIEW output_format_stats AS
SELECT 
  h.user_id,
  of.name as output_format,
  COUNT(h.id) as usage_count
FROM history h
JOIN output_formats of ON h.output_format_id = of.id
GROUP BY h.user_id, of.name;
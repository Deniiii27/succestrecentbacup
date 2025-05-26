/*
  # Initial Database Schema

  1. New Tables
    - users
      - id (uuid, primary key)
      - username (text, unique)
      - password (text)
      - email (text, unique)
      - full_name (text)
      - created_at (timestamptz)
      - last_login_at (timestamptz)
      - is_active (boolean)
    
    - folders
      - id (uuid, primary key)
      - user_id (uuid, references users)
      - name (text)
      - created_at (timestamptz)
      - updated_at (timestamptz)
    
    - file_types
      - id (serial, primary key)
      - name (text, unique)
    
    - output_formats
      - id (serial, primary key)
      - name (text, unique)
    
    - history
      - id (uuid, primary key)
      - user_id (uuid, references users)
      - process_date (timestamptz)
      - input_file_type_id (int, references file_types)
      - output_format_id (int, references output_formats)
      - processing_time (int)
      - prompt_text (text)
      - process_type (text)
      - is_success (boolean)
    
    - output_files
      - id (uuid, primary key)
      - history_id (uuid, references history)
      - name (text)
      - path (text)
      - size (bigint)
      - created_at (timestamptz)
      - folder_id (uuid, references folders)
    
    - charts
      - id (uuid, primary key)
      - user_id (uuid, references users)
      - type (text)
      - name (text)
      - last_generated_at (timestamptz)

  2. Security
    - Enable RLS on all tables
    - Add policies for authenticated users to access their own data
    - Public can read file_types and output_formats

  3. Initial Data
    - Insert default file types
    - Insert default output formats
*/

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create users table
CREATE TABLE users (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  username text UNIQUE NOT NULL,
  password text NOT NULL,
  email text UNIQUE NOT NULL,
  full_name text,
  created_at timestamptz DEFAULT now(),
  last_login_at timestamptz,
  is_active boolean DEFAULT true
);

-- Create folders table
CREATE TABLE folders (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id uuid REFERENCES users(id) ON DELETE CASCADE,
  name text NOT NULL,
  created_at timestamptz DEFAULT now(),
  updated_at timestamptz DEFAULT now()
);

-- Create file_types table
CREATE TABLE file_types (
  id serial PRIMARY KEY,
  name text UNIQUE NOT NULL
);

-- Create output_formats table
CREATE TABLE output_formats (
  id serial PRIMARY KEY,
  name text UNIQUE NOT NULL
);

-- Create history table
CREATE TABLE history (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id uuid REFERENCES users(id) ON DELETE CASCADE,
  process_date timestamptz DEFAULT now(),
  input_file_type_id int REFERENCES file_types(id),
  output_format_id int REFERENCES output_formats(id),
  processing_time int,
  prompt_text text,
  process_type text,
  is_success boolean DEFAULT true
);

-- Create output_files table
CREATE TABLE output_files (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  history_id uuid REFERENCES history(id) ON DELETE CASCADE,
  name text NOT NULL,
  path text NOT NULL,
  size bigint,
  created_at timestamptz DEFAULT now(),
  folder_id uuid REFERENCES folders(id) ON DELETE SET NULL
);

-- Create charts table
CREATE TABLE charts (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id uuid REFERENCES users(id) ON DELETE CASCADE,
  type text NOT NULL,
  name text NOT NULL,
  last_generated_at timestamptz DEFAULT now()
);

-- Enable Row Level Security
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE folders ENABLE ROW LEVEL SECURITY;
ALTER TABLE history ENABLE ROW LEVEL SECURITY;
ALTER TABLE output_files ENABLE ROW LEVEL SECURITY;
ALTER TABLE charts ENABLE ROW LEVEL SECURITY;

-- RLS Policies
-- Users can only access their own data
CREATE POLICY "Users can access own data" ON users
  FOR ALL USING (auth.uid() = id);

-- Folders policies
CREATE POLICY "Users can access own folders" ON folders
  FOR ALL USING (auth.uid() = user_id);

-- History policies
CREATE POLICY "Users can access own history" ON history
  FOR ALL USING (auth.uid() = user_id);

-- Output files policies
CREATE POLICY "Users can access own output files" ON output_files
  FOR ALL USING (
    auth.uid() IN (
      SELECT user_id FROM history WHERE id = output_files.history_id
    )
  );

-- Charts policies
CREATE POLICY "Users can access own charts" ON charts
  FOR ALL USING (auth.uid() = user_id);

-- File types and output formats are public readable
ALTER TABLE file_types ENABLE ROW LEVEL SECURITY;
ALTER TABLE output_formats ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Anyone can read file types" ON file_types
  FOR SELECT USING (true);

CREATE POLICY "Anyone can read output formats" ON output_formats
  FOR SELECT USING (true);

-- Insert initial data
INSERT INTO file_types (name) VALUES 
  ('PDF'), ('DOCX'), ('XLSX'), ('PNG'), ('JPG'), ('PROMPT'), ('OTHER');

INSERT INTO output_formats (name) VALUES 
  ('Excel'), ('Word');

-- Create functions for stored procedures
CREATE OR REPLACE FUNCTION user_login(p_username text, p_password text)
RETURNS TABLE (
  id uuid,
  username text,
  email text,
  full_name text
) AS $$
BEGIN
  -- Update last login time
  UPDATE users 
  SET last_login_at = now()
  WHERE username = p_username 
    AND password = p_password 
    AND is_active = true;
    
  -- Return user data
  RETURN QUERY
  SELECT 
    users.id,
    users.username,
    users.email,
    users.full_name
  FROM users
  WHERE username = p_username 
    AND password = p_password 
    AND is_active = true;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Create views for statistics
CREATE VIEW input_file_type_stats AS
SELECT 
  h.user_id,
  ft.name AS file_type,
  COUNT(h.id) AS usage_count
FROM history h
JOIN file_types ft ON h.input_file_type_id = ft.id
GROUP BY h.user_id, ft.name;

CREATE VIEW output_format_stats AS
SELECT 
  h.user_id,
  of.name AS output_format,
  COUNT(h.id) AS usage_count
FROM history h
JOIN output_formats of ON h.output_format_id = of.id
GROUP BY h.user_id, of.name;
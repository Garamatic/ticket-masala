
import os

REPLACEMENTS = [
    ("TicketMasala.Web.Services.Rules", "TicketMasala.Web.Engine.Compiler"),
    ("TicketMasala.Web.Services.Ingestion", "TicketMasala.Web.Engine.Ingestion"),
    ("TicketMasala.Web.Services.GERDA", "TicketMasala.Web.Engine.GERDA"),
]

def update_nested(root_dir):
    count = 0
    for root, dirs, files in os.walk(root_dir):
        for file in files:
            if file.endswith(".cs") or file.endswith(".cshtml"):
                filepath = os.path.join(root, file)
                try:
                    with open(filepath, 'r', encoding='utf-8') as f:
                        content = f.read()
                    
                    original_content = content
                    for old, new in REPLACEMENTS:
                        content = content.replace(old, new)
                    
                    if content != original_content:
                       with open(filepath, 'w', encoding='utf-8') as f:
                           f.write(content)
                       print(f"[UPDATED] {filepath}")
                       count += 1
                except Exception as e:
                    print(f"[ERROR] {filepath}: {e}")
    print(f"Updated nested namespaces in {count} files.")

if __name__ == "__main__":
    update_nested("src")

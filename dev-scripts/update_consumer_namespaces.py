
import os

REPLACEMENTS = [
    ("using TicketMasala.Web.Services.Rules;", "using TicketMasala.Web.Engine.Compiler;"),
    ("using TicketMasala.Web.Services.Ingestion;", "using TicketMasala.Web.Engine.Ingestion;"),
    ("using TicketMasala.Web.Services.GERDA;", "using TicketMasala.Web.Engine.GERDA;"),
    # Also handle variants without semicolon if any (though unlikely for using)
]

def update_consumers(root_dir):
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
    print(f"Updated consumers in {count} files.")

if __name__ == "__main__":
    update_consumers("src")

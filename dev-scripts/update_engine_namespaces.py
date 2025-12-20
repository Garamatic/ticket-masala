
import os
import re

MAPPINGS = [
    ("TicketMasala.Web.Services.Rules", "TicketMasala.Web.Engine.Compiler"),
    ("TicketMasala.Web.Services.Ingestion", "TicketMasala.Web.Engine.Ingestion"),
    ("TicketMasala.Web.Services.GERDA", "TicketMasala.Web.Engine.GERDA"),
]

def update_namespaces(root_dir):
    count = 0
    for root, dirs, files in os.walk(root_dir):
        for file in files:
            if file.endswith(".cs"):
                filepath = os.path.join(root, file)
                try:
                    with open(filepath, 'r', encoding='utf-8') as f:
                        content = f.read()
                    
                    original_content = content
                    for old_ns, new_ns in MAPPINGS:
                        # Replace namespace definition
                        content = content.replace(f"namespace {old_ns}", f"namespace {new_ns}")
                        # Replace usings if any (though these files themselves might be the definition)
                        # We also need to be careful about other files referencing these.
                    
                    if content != original_content:
                       with open(filepath, 'w', encoding='utf-8') as f:
                           f.write(content)
                       print(f"[UPDATED] {filepath}")
                       count += 1
                except Exception as e:
                    print(f"[ERROR] {filepath}: {e}")
    print(f"Updated namespaces in {count} files.")

if __name__ == "__main__":
    update_namespaces("src/TicketMasala.Web/Engine")

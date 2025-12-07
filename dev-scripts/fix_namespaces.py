import os
import fileinput

# DEFINING THE MIGRATION RULES
# Order matters: We replace more specific (Tests) before generic (Web)
# to avoid partial matches creating invalid namespaces.
REPLACEMENTS = [
    # 1. Fix the Test Project References
    ("IT_Project2526.Tests", "TicketMasala.Tests"),
    ("IT-Project2526.Tests", "TicketMasala.Tests"),
    
    # 2. Fix the Main Web Project References
    ("namespace IT_Project2526", "namespace TicketMasala.Web"),
    ("using IT_Project2526", "using TicketMasala.Web"),
    ("IT-Project2526", "TicketMasala.Web"), # For .sln and .csproj text references
    ("IT_Project2526", "TicketMasala.Web"), # Catch-all for other code references
]

TARGET_EXTENSIONS = {'.cs', '.cshtml', '.json', '.sln', '.csproj', '.xml'}

def process_file(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()

        original_content = content
        
        # Apply all replacements in order
        for old_str, new_str in REPLACEMENTS:
            content = content.replace(old_str, new_str)

        # Only write if changes were made
        if content != original_content:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"[FIXED] {filepath}")
            return 1
        return 0

    except Exception as e:
        print(f"[ERROR] Could not process {filepath}: {e}")
        return 0

def main():
    print("--- STARTING TICKET MASALA MIGRATION ---")
    
    # We walk from the current directory downwards
    root_dir = "." 
    count = 0
    
    for root, dirs, files in os.walk(root_dir):
        # Skip .git and bin/obj folders to save time and avoid locking issues
        if '.git' in root or '\\bin' in root or '\\obj' in root or '/bin' in root or '/obj' in root:
            continue
            
        for file in files:
            # Check extension
            ext = os.path.splitext(file)[1]
            if ext in TARGET_EXTENSIONS:
                filepath = os.path.join(root, file)
                count += process_file(filepath)

    print("-" * 30)
    print(f"Migration Complete. Modified {count} files.")
    print("NEXT STEP: Run 'dotnet build' to verify integrity.")

if __name__ == "__main__":
    main()
import os
import re

# CONFIGURATION
# We assume you have already renamed the folders to TicketMasala.Web / TicketMasala.Tests
SOURCE_MAPPING = {
    # (Old Namespace Regex) : (New Namespace String)
    r"namespace\s+IT[-_]Project2526\.Tests": "namespace TicketMasala.Tests",
    r"namespace\s+IT[-_]Project2526": "namespace TicketMasala.Web",
}

TEXT_REPLACEMENTS = [
    ("IT-Project2526", "TicketMasala.Web"),
    ("IT_Project2526", "TicketMasala.Web"),
    ("IT-Project2526.Tests", "TicketMasala.Tests"),
    ("IT_Project2526.Tests", "TicketMasala.Tests"),
]

EXTENSIONS = {'.cs', '.cshtml', '.json', '.xml', '.sln', '.csproj'}

def convert_namespace_and_rename(content):
    modified = False
    original_content = content

    # 1. NAMESPACE MODERNIZATION (Block -> File-Scoped)
    # We look for: namespace X {
    # We change to: namespace Y;
    
    for old_pattern, new_namespace in SOURCE_MAPPING.items():
        # Regex explanation:
        # namespace\s+...  -> Finds the namespace declaration
        # \s*\{?           -> Optionally matches whitespace and an opening brace
        match = re.search(old_pattern + r"\s*(\{)?", content)
        
        if match:
            # We found a namespace declaration.
            is_block_scoped = match.group(1) == '{' or (content.find('{', match.end()) != -1 and content.find('{', match.end()) < content.find('\n', match.end()) + 50)
            
            # Replace the declaration line with the new file-scoped format
            # We use a regex sub to replace the whole definition line
            content = re.sub(old_pattern + r"[\s\r\n]*\{?", f"{new_namespace};", content, count=1)
            
            if is_block_scoped:
                # If it was block scoped, we effectively removed the opening '{'.
                # We must now remove the corresponding closing '}' at the end of the file.
                # We don't count braces. We assume the last '}' is the namespace closer.
                last_brace_index = content.rfind('}')
                if last_brace_index != -1:
                    # Remove the brace
                    content = content[:last_brace_index] + content[last_brace_index+1:]
            
            modified = True
            break # Only one namespace per file

    # 2. GENERAL TEXT REPLACEMENT (Using statements, etc.)
    for old_text, new_text in TEXT_REPLACEMENTS:
        if old_text in content:
            content = content.replace(old_text, new_text)
            modified = True

    return content, modified

def process_file(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()

        new_content, was_modified = convert_namespace_and_rename(content)

        if was_modified:
            # Cleanup: The removal of the last } might leave trailing whitespace
            new_content = new_content.rstrip() + "\n"
            
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(new_content)
            print(f"[UPDATED] {filepath}")
            return 1
    except Exception as e:
        print(f"[ERROR] {filepath}: {e}")
    return 0

def main():
    print("=== STARTING TICKET MASALA MIGRATION v2 ===")
    count = 0
    # Walk the directory
    for root, dirs, files in os.walk("."):
        if '.git' in root or 'bin' in root or 'obj' in root:
            continue
            
        for file in files:
            if os.path.splitext(file)[1] in EXTENSIONS:
                count += process_file(os.path.join(root, file))
                
    print(f"=== COMPLETE. Modified {count} files. ===")
    print("Run 'dotnet build' to verify.")

if __name__ == "__main__":
    main()

import os
import re

def fix_file(filepath):
    try:
        # Use utf-8-sig to handle BOM
        with open(filepath, 'r', encoding='utf-8-sig') as f:
            lines = f.readlines()
        
        # We need to detect BOM in original file to preserve it? 
        # Actually utf-8-sig removes BOM on read. If we write utf-8, it loses BOM. That's fine for C# generally.
        
        modified = False
        new_lines = []
        
        for line in lines:
            # Fix malformed namespace: "namespace TicketMasala.Web;.Suffix"
            if "namespace TicketMasala" in line and ";." in line:
                line = line.replace(";. ", ".").replace(";.", ".")
                if not line.strip().endswith(";"):
                    # ensure file scoped end
                    line = line.rstrip() + ";\n"
                modified = True
            
            # Ensure file scoped syntax ends with ;
            if line.strip().startswith("namespace TicketMasala") and not line.strip().endswith(";") and "{" not in line:
                 line = line.rstrip() + ";\n"
                 modified = True
                 
            new_lines.append(line)

        # Re-join to do brace checks on full content
        content = "".join(new_lines)
        
        # Check for block brace after file-scoped namespace
        # Pattern: namespace X;\n{
        namespace_match = re.search(r'namespace\s+[\w.]+\s*;\s*(\{)', content, re.DOTALL)
        
        if namespace_match:
            # Found "namespace X; {"
            # We need to remove that '{' line/char
            # And the last '}' in the file
            
            final_lines = []
            brace_removed = False
            
            # Find the brace line index
            # We assume it's roughly after the namespace line
            # Locate namespace line index
            ns_idx = -1
            for i, l in enumerate(new_lines):
                if l.strip().startswith("namespace TicketMasala") and l.strip().endswith(";"):
                    ns_idx = i
                    break
            
            if ns_idx != -1:
                # Search forward for {
                open_brace_idx = -1
                for i in range(ns_idx, len(new_lines)):
                    if new_lines[i].strip() == "{":
                        open_brace_idx = i
                        break
                    if "{" in new_lines[i] and new_lines[i].strip() != "{":
                         # Inline brace? "namespace X; {" -> "namespace X;"
                         # Handle replacement
                         pass
                
                if open_brace_idx != -1:
                    new_lines.pop(open_brace_idx)
                    brace_removed = True
                    modified = True
            
            if brace_removed:
                 # Remove last closing brace
                 for i in range(len(new_lines) - 1, -1, -1):
                     if new_lines[i].strip() == "}":
                         new_lines.pop(i)
                         modified = True
                         break

        if modified:
             with open(filepath, 'w', encoding='utf-8') as f:
                f.writelines(new_lines)
             print(f"[FIXED] {filepath}")
             return 1
             
    except Exception as e:
        print(f"[ERROR] {filepath}: {e}")
        import traceback
        traceback.print_exc()
    return 0

count = 0
for root, dirs, files in os.walk("src"):
    for file in files:
        if file.endswith(".cs"):
            count += fix_file(os.path.join(root, file))
print(f"Fixed {count} files.")

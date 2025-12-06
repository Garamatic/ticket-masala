
import os

def balance_braces(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8-sig') as f:
            lines = f.readlines()
            content = "".join(lines)

        open_count = content.count('{')
        close_count = content.count('}')

        diff = open_count - close_count

        if diff > 0:
            with open(filepath, 'a', encoding='utf-8') as f: # Write as utf-8 (no BOM usually appended but ok)
                if lines and not lines[-1].endswith('\n'):
                    f.write('\n')
                f.write('}\n' * diff)
            print(f"[BALANCED] {filepath}: Added {diff} }}")
            return 1
             
    except Exception as e:
        print(f"[ERROR] {filepath}: {e}")
    return 0

count = 0
for root, dirs, files in os.walk("src"):
    for file in files:
        if file.endswith(".cs"):
            count += balance_braces(os.path.join(root, file))
print(f"Balanced {count} files.")

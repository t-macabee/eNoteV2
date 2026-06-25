import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))  
ENOTE_DIR = os.path.dirname(SCRIPT_DIR)                 
REPO_ROOT = os.path.dirname(ENOTE_DIR)                  

SOURCE_DIRS = [
    os.path.join(ENOTE_DIR, 'eNote.Domain'),
    os.path.join(ENOTE_DIR, 'eNote.Application'),
    os.path.join(ENOTE_DIR, 'eNote.Infrastructure'),
    os.path.join(ENOTE_DIR, 'eNote.API'),
    os.path.join(ENOTE_DIR, 'eNote.Contracts')
]

OUTPUT_DIR = os.path.join(REPO_ROOT, '.docs', 'blueprint')
DOMAINS = ['Auth', 'InstrumentRentals', 'Assignments', 'Courses', 'Payments']

ALLOWED_EXTENSIONS = {'.cs', '.json'}
IGNORE_KEYWORDS = ['bin', 'obj', 'Migrations', 'Properties', 'assets', 'build']

def should_ignore(path):
    return any(keyword in path for keyword in IGNORE_KEYWORDS)

def get_domain_for_file(filepath, filename):
    normalized_path = filepath.replace('\\', '/').lower()
    filename_lower = filename.lower()

    for domain in DOMAINS:
        dom_lower = domain.lower()
        if f"/{dom_lower}/" in normalized_path or f".{dom_lower}/" in normalized_path or f"/{dom_lower}." in normalized_path:
            return domain
            
    for domain in DOMAINS:
        if domain.lower() in filename_lower:
            return domain
            
    return 'Shared_Infrastructure'

def generate_system_overview(domain_counts):
    """Generates a high-level system index map to prevent Claude from getting confused."""
    overview_path = os.path.join(OUTPUT_DIR, '00_System_Overview.md')
    
    content = [
        "# 🗺️ System Architecture Overview & Index\n",
        "This master index serves as Claude's top-level mental map of the system architecture.\n",
        "## 🏗️ Architectural Layers Hierarchy (Clean Architecture)",
        "The system strictly follows dependency injection patterns flowing inwards:",
        "```text",
        "  eNote.API (Presentation) ──> eNote.Infrastructure ──┐",
        "             │                                        ▼",
        "             └─────────> eNote.Application ──> eNote.Domain",
        "```\n",
        "## 🔀 Asynchronous Communication Pattern",
        "* **Event Bus:** Cross-domain integration events are decoupled via **RabbitMQ**.",
        "* **Flow Example:** When `Domain_InstrumentRentals` fires an execution state change event, a background consumer in the isolated Worker container processes it asynchronously to trigger notifications[cite: 3].\n",
        "## 📦 Identified Bounded Contexts & File Distribution",
        "Use these specific domain files when pinning context in Claude or Cursor:\n"
    ]
    
    for domain in DOMAINS:
        count = domain_counts.get(domain, 0)
        content.append(f"* **`@Domain_{domain}.md`**: Contains the core logic, features, DTOs, and contracts for the **{domain}** context ({count} source files).")
        
    shared_count = domain_counts.get('Shared_Infrastructure', 0)
    content.append(f"* **`@Domain_Shared_Infrastructure.md`**: Fallback directory for shared configurations, generic middleware, and foundational cross-cutting components ({shared_count} files)[cite: 3].")
    
    with open(overview_path, 'w', encoding='utf-8') as f:
        f.write("\n".join(content))
    print(f"Generated System Map: {overview_path}")

def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    domain_buffers = {domain: [] for domain in DOMAINS}
    domain_buffers['Shared_Infrastructure'] = []
    domain_counts = {domain: 0 for domain in DOMAINS}
    domain_counts['Shared_Infrastructure'] = 0
    
    print("Parsing codebase for Claude blueprint generation...")
    
    for s_dir in SOURCE_DIRS:
        if not os.path.exists(s_dir):
            continue
            
        for root, _, files in os.walk(s_dir):
            if should_ignore(root):
                continue
                
            for file in files:
                _, ext = os.path.splitext(file)
                if ext not in ALLOWED_EXTENSIONS:
                    continue
                    
                filepath = os.path.join(root, file)
                domain = get_domain_for_file(filepath, file)
                
                try:
                    with open(filepath, 'r', encoding='utf-8') as f:
                        content = f.read()
                        
                    rel_path = os.path.relpath(filepath, REPO_ROOT)
                    entry = f"## File: {rel_path}\n"
                    entry += f"```{ext[1:]}\n{content}\n```\n\n"
                    domain_buffers[domain].append(entry)
                    domain_counts[domain] += 1
                except Exception as e:
                    print(f"Skipping {filepath} due to error: {e}")

    for domain, blocks in domain_buffers.items():
        if not blocks:
            continue
            
        output_file = os.path.join(OUTPUT_DIR, f"Domain_{domain}.md")
        with open(output_file, 'w', encoding='utf-8') as out:
            out.write(f"# Bounded Context: {domain}\n")
            out.write(f"Total Files Contained: {len(blocks)}\n")
            out.write("---\n\n")
            out.write("".join(blocks))
        print(f"Generated Domain Block: {output_file} ({len(blocks)} files)")

    generate_system_overview(domain_counts)

if __name__ == "__main__":
    main()
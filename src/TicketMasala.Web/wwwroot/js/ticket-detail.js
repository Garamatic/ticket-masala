document.addEventListener('DOMContentLoaded', function() {
    function formatComments() {
        document.querySelectorAll('.comment-content').forEach(el => {
            // Only process if not already processed (check if it has children or inner HTML is different from raw)
            // Or simpler: check a data attribute?
            // Actually, we can just checking if it is empty since the Partial renders clean divs?
            // Wait, the partial renders: <div ... data-content="..."></div>?
            // Yes.
            if (!el.innerHTML.trim()) {
                const rawContent = el.getAttribute('data-content');
                if (rawContent && window.marked) {
                    // Simple mention linking: @Name -> <span class="text-primary">@Name</span>
                    const withMentions = rawContent.replace(/@(\w+)/g, '<span class="fw-bold text-primary">@$1</span>');
                    el.innerHTML = marked.parse(withMentions);
                }
            }
        });
    }

    // Initial load
    formatComments();

    // HTMX updates
    document.body.addEventListener('htmx:afterSwap', function(evt) {
        if (evt.target.id === 'commentList' || evt.target.querySelector('#commentList')) {
            formatComments();
        }
    });

    // Editor Toolbar
    const textarea = document.getElementById('commentBody');
    
    if (textarea) {
        window.insertFormat = function(format) {
            const start = textarea.selectionStart;
            const end = textarea.selectionEnd;
            const text = textarea.value;
            const before = text.substring(0, start);
            const selection = text.substring(start, end);
            const after = text.substring(end);
            
            let newText = '';
            if (format === 'bold') newText = `**${selection || 'bold'}**`;
            if (format === 'italic') newText = `*${selection || 'italic'}*`;
            if (format === 'list') newText = `\n- ${selection || 'item'}`;
            
            textarea.value = before + newText + after;
            textarea.focus();
        };

        window.insertMention = function(username) {
            const start = textarea.selectionStart;
            const text = textarea.value;
            const before = text.substring(0, start);
            const after = text.substring(start);
            
            textarea.value = before + `@${username} ` + after;
            textarea.focus();
        };
    }
});

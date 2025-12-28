function preprocessCustomLatexTags(root) {
    if (!root) return;
    try {
        // Replace [latex]...[/latex] with span to allow auto-render to process inner content
        const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, null);
        const nodesToProcess = [];
        while (walker.nextNode()) {
            const n = walker.currentNode;
            if (!n.nodeValue) continue;
            if (n.nodeValue.match(/\[latex\]([\s\S]*?)\[\/latex\]/i)) {
                nodesToProcess.push(n);
            }
        }
        nodesToProcess.forEach(textNode => {
            const html = textNode.nodeValue.replace(/\[latex\]([\s\S]*?)\[\/latex\]/gi, function (_, inner) {
                // Keep inner delimiters ($$, $, \[, \() to let auto-render decide display vs inline
                // decode HTML entities first because the text node might have been escaped
                let decoded = inner
                    .replace(/&amp;/g, '&')
                    .replace(/&lt;/g, '<')
                    .replace(/&gt;/g, '>')
                    .replace(/&quot;/g, '"')
                    .replace(/&#39;/g, "'");

                // Encode specifically for KaTeX to handle & and other chars correctly in math mode
                const encoded = decoded
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;');
                return '<span class="katex-custom">' + encoded + '</span>';
            });
            const span = document.createElement('span');
            span.innerHTML = html;
            textNode.parentNode.replaceChild(span, textNode);
        });
    } catch (_) { }
}

window.katexInterop = {
    renderAllInSelector: function (selector) {
        try {
            if (!window.renderMathInElement) return;
            document.querySelectorAll(selector).forEach(function (el) {
                preprocessCustomLatexTags(el);
                window.renderMathInElement(el, {
                    delimiters: [
                        { left: "$$", right: "$$", display: true },
                        { left: "\\[", right: "\\]", display: true },
                        { left: "$", right: "$", display: false },
                        { left: "\\(", right: "\\)", display: false }
                    ],
                    throwOnError: false
                });
            });
        } catch (e) { }
    },
    renderAllInElement: function (element) {
        try {
            if (!window.renderMathInElement) return;
            preprocessCustomLatexTags(element);
            window.renderMathInElement(element, {
                delimiters: [
                    { left: "$$", right: "$$", display: true },
                    { left: "\\[", right: "\\]", display: true },
                    { left: "$", right: "$", display: false },
                    { left: "\\(", right: "\\)", display: false }
                ],
                throwOnError: false
            });
        } catch (e) { }
    }
};



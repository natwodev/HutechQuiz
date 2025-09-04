var MathJax = window.MathJax;
var promise = new Promise(function (resolved, rej) { resolved(); });

// Đảm bảo MathJax đã được load
window.mathJaxBlazor = {
    applySettings: function(texSettings) {
        MathJax.config.tex.inlineMath = texSettings.inlineMath;
        MathJax.config.tex.displayMath = texSettings.displayMath;
        //MathJax.startup.input[0].findTeX.options.processEscapes = texSettings.processEscapes;
        //MathJax.startup.input[0].findTeX.options.processEnvironments = texSettings.processEnvironments;
        //MathJax.startup.input[0].findTeX.options.processRefs = texSettings.processRefs;
        MathJax.startup.getComponents();
    },

    typesetPromise: function() {
        promise = promise.then(function () {
            this.typesetClear();
            return MathJax.typesetPromise();
        }.bind(this)).catch(function (err) {
            // Error occurred during typeset
        });
    },

    typesetClear: function() {
        try {
            this.undoTypeset();
            //MathJax.startup.document.state(0);
            MathJax.texReset();
            MathJax.typesetClear();
            MathJax.startup.document.clear();
        }
        catch (ex) {
            // Error occurred during typeset clear
        }
    },

    undoTypeset: function() {
        var list = MathJax.startup.document.getMathItemsWithin(document.body);
        //var list = MathJax.startup.document.math.toArray();  // does not work anymore.
        for (var i = 0; i < list.length; i++) {
            list[i].start.node.outerHTML = list[i].start.delim + list[i].math + list[i].end.delim;
        }
    },

    processLatex: function(input, isDisplay) {
        if (typeof MathJax === 'undefined' || !MathJax.tex2chtmlPromise) {
            return Promise.resolve(""); // Trả về chuỗi rỗng thay vì lỗi
        }

        return MathJax.tex2chtmlPromise(input, { display: isDisplay })
            .then(function (node) {
                MathJax.startup.document.clear();
                MathJax.startup.document.updateDocument();
                return node.outerHTML;
            })
            .catch(function (err) {
                return "";
            });
    },

    processMathML: function(input) {
        if (typeof MathJax === 'undefined' || !MathJax.mathml2chtmlPromise) {
            return Promise.resolve("");
        }

        return MathJax.mathml2chtmlPromise(input)
            .then(function (node) {
                MathJax.startup.document.clear();
                MathJax.startup.document.updateDocument();
                return node.outerHTML;
            })
            .catch(function (err) {
                return "";
            });
    }
};

//# sourceMappingURL=mathJaxBlazor.js.map

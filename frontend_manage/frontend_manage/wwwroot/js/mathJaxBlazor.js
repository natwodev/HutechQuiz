var MathJax = window.MathJax;
var promise = new Promise(function (resolved, rej) { resolved(); });
export function applySettings(texSettings) {
    MathJax.config.tex.inlineMath = texSettings.inlineMath;
    MathJax.config.tex.displayMath = texSettings.displayMath;
    //MathJax.startup.input[0].findTeX.options.processEscapes = texSettings.processEscapes;
    //MathJax.startup.input[0].findTeX.options.processEnvironments = texSettings.processEnvironments;
    //MathJax.startup.input[0].findTeX.options.processRefs = texSettings.processRefs;
    MathJax.startup.getComponents();
}
export function typesetPromise() {
    promise = promise.then(function () {
        typesetClear();
        return MathJax.typesetPromise();
    }).catch(function (err) {
        console.log(err);
    });
}
export function typesetClear() {
    try {
        undoTypeset();
        //MathJax.startup.document.state(0);
        MathJax.texReset();
        MathJax.typesetClear();
        MathJax.startup.document.clear();
    }
    catch (ex) {
        console.log(ex);
    }
}
export function undoTypeset() {
    var list = MathJax.startup.document.getMathItemsWithin(document.body);
    //var list = MathJax.startup.document.math.toArray();  // does not work anymore.
    for (var i = 0; i < list.length; i++) {
        list[i].start.node.outerHTML = list[i].start.delim + list[i].math + list[i].end.delim;
    }
}
export function processLatex(input, isDisplay) {
    if (typeof MathJax === 'undefined' || !MathJax.tex2chtmlPromise) {
        console.error("MathJax chưa được khởi tạo hoặc tex2chtmlPromise không tồn tại!");
        return Promise.resolve(""); // Trả về chuỗi rỗng thay vì lỗi
    }

    return MathJax.tex2chtmlPromise(input, { display: isDisplay })
        .then(function (node) {
            MathJax.startup.document.clear();
            MathJax.startup.document.updateDocument();
            return node.outerHTML;
        })
        .catch(function (err) {
            console.error("Lỗi khi xử lý LaTeX:", err);
            return "";
        });
}

export function processMathML(input) {
    if (typeof MathJax === 'undefined' || !MathJax.mathml2chtmlPromise) {
        console.error("MathJax chưa được khởi tạo hoặc mathml2chtmlPromise không tồn tại!");
        return Promise.resolve("");
    }

    return MathJax.mathml2chtmlPromise(input)
        .then(function (node) {
            MathJax.startup.document.clear();
            MathJax.startup.document.updateDocument();
            return node.outerHTML;
        })
        .catch(function (err) {
            console.error("Lỗi khi xử lý MathML:", err);
            return "";
        });
}

//# sourceMappingURL=mathJaxBlazor.js.map

var __matchingLines = {};
function __getStore(containerId){
    if (!__matchingLines[containerId]) __matchingLines[containerId] = [];
    return __matchingLines[containerId];
}
window.matchingHelpers = {
    getRect: function(id) {
        const el = document.getElementById(id);
        if (!el) return null;
        const r = el.getBoundingClientRect();
        return { x: r.left + r.width/2 + window.scrollX, y: r.top + r.height/2 + window.scrollY, w: r.width, h: r.height };
    },
    getRelativeLine: function(containerId, leftId, rightId) {
        const container = document.getElementById(containerId);
        const l = document.getElementById(leftId);
        const r = document.getElementById(rightId);
        if (!container || !l || !r) return null;
        const cr = container.getBoundingClientRect();
        const lr = l.getBoundingClientRect();
        const rr = r.getBoundingClientRect();
        const x1 = lr.right - cr.left;
        const y1 = lr.top + lr.height/2 - cr.top;
        const x2 = rr.left - cr.left;
        const y2 = rr.top + rr.height/2 - cr.top;
        return [x1, y1, x2, y2, cr.width, cr.height];
    },
    drawLeaderLine: function (leftId, rightId) {
        function ensureLeaderLine(cb){
            if (window.LeaderLine) { cb(); return; }
            var s = document.createElement('script');
            s.src = 'https://unpkg.com/leader-line@1.0.7/leader-line.min.js';
            s.onload = cb;
            document.body.appendChild(s);
        }
        const l = document.getElementById(leftId);
        const r = document.getElementById(rightId);
        if (!l || !r) {
            console.warn('Matching elements not found', leftId, rightId);
            return null;
        }
        ensureLeaderLine(function(){
            setTimeout(function(){
                const line = new LeaderLine(l, r, { color: '#ef4444', size: 2, startPlug: 'disc', endPlug: 'arrow3', path: 'straight' });
                if (line && line.svg && line.svg.style) {
                    line.svg.style.zIndex = 9999;
                }
            }, 0);
        });
        return true;
    },
    addLeaderLine: function(containerId, leftId, rightId){
        function ensure(cb){
            if (window.LeaderLine) { cb(); return; }
            var s = document.createElement('script');
            s.src = 'https://unpkg.com/leader-line@1.0.7/leader-line.min.js';
            s.onload = cb;
            document.body.appendChild(s);
        }
        ensure(function(){
            var l = document.getElementById(leftId);
            var r = document.getElementById(rightId);
            if (!l || !r) return;
            // remove existing lines referencing same left or right
            window.matchingHelpers.removeLeaderLineByLeft(containerId, leftId);
            window.matchingHelpers.removeLeaderLineByRight(containerId, rightId);
            var line = new LeaderLine(l, r, { color: '#ef4444', size: 2, startPlug: 'disc', endPlug: 'arrow3', path: 'straight' });
            var store = __getStore(containerId);
            store.push({ leftId: leftId, rightId: rightId, line: line });
            if (line && line.svg && line.svg.style) line.svg.style.zIndex = 9999;
        });
    },
    redrawLeaderLines: function(containerId){
        var arr = __matchingLines[containerId];
        if (!arr) return;
        for (var i=0;i<arr.length;i++){ try{ arr[i].line.position(); }catch(e){} }
    },
    clearLeaderLines: function(containerId){
        var arr = __matchingLines[containerId];
        if (!arr) return;
        for (var i=0;i<arr.length;i++){ try{ arr[i].line.remove(); }catch(e){} }
        delete __matchingLines[containerId];
    },
    removeLeaderLineByLeft: function(containerId, leftId){
        var arr = __getStore(containerId);
        for (var i = arr.length - 1; i >= 0; i--) {
            if (arr[i].leftId === leftId) { try{ arr[i].line.remove(); }catch(e){} arr.splice(i,1); }
        }
    },
    removeLeaderLineByRight: function(containerId, rightId){
        var arr = __getStore(containerId);
        for (var i = arr.length - 1; i >= 0; i--) {
            if (arr[i].rightId === rightId) { try{ arr[i].line.remove(); }catch(e){} arr.splice(i,1); }
        }
    }
};



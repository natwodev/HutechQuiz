var __matchingLines = {};

function __getStore(containerId) {
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
        function ensureLeaderLine(cb) {
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

        ensureLeaderLine(function() {
            setTimeout(function() {
                const line = new LeaderLine(l, r, { 
                    color: '#ef4444', 
                    size: 2, 
                    startPlug: 'disc', 
                    endPlug: 'arrow3', 
                    path: 'straight'
                });
                
                if (line && line.svg && line.svg.style) {
                    line.svg.style.zIndex = '99999';
                    line.svg.style.pointerEvents = 'none';
                }
            }, 0);
        });

        return true;
    },

    addLeaderLine: function(containerId, leftId, rightId) {
        function ensure(cb) {
            if (window.LeaderLine) { cb(); return; }
            var s = document.createElement('script');
            s.src = 'https://unpkg.com/leader-line@1.0.7/leader-line.min.js';
            s.onload = cb;
            document.body.appendChild(s);
        }

        ensure(function() {
            try {
                var l = document.getElementById(leftId);
                var r = document.getElementById(rightId);
                if (!l || !r) return;

                // Remove existing lines referencing same left or right TRƯỚC KHI tạo line mới
                window.matchingHelpers.removeLeaderLineByLeft(containerId, leftId);
                window.matchingHelpers.removeLeaderLineByRight(containerId, rightId);

                // Đợi một chút để đảm bảo lines cũ đã được xóa
                setTimeout(function() {
                    try {
                        // Tạo line mới - LeaderLine tự động append vào body
                        var line = new LeaderLine(l, r, { 
                            color: '#ef4444', 
                            size: 2, 
                            startPlug: 'disc', 
                            endPlug: 'arrow3', 
                            path: 'straight'
                        });

                        if (line && line.svg) {
                            // Đánh dấu SVG
                            line.svg.setAttribute('data-container-id', containerId);
                            line.svg.setAttribute('data-leader-line', 'true');

                            // Chỉ set z-index và pointer-events, KHÔNG set position
                            if (line.svg.style) {
                                line.svg.style.zIndex = '99999';
                                line.svg.style.pointerEvents = 'none';
                                // Đảm bảo không có position fixed
                                line.svg.style.position = '';
                                // Đảm bảo hiển thị trong fullscreen
                                line.svg.style.display = 'block';
                                line.svg.style.visibility = 'visible';
                                line.svg.style.opacity = '1';
                            }
                        }

                        // Lắng nghe scroll để cập nhật line position
                        // LeaderLine tự động cập nhật, nhưng cần đảm bảo nó hoạt động với scroll trong container
                        var updateLineOnScroll = function() {
                            try {
                                if (line && line.position) {
                                    line.position();
                                }
                            } catch (e) {
                                // Ignore errors
                            }
                        };
                        
                        // Lắng nghe scroll trên window (scroll trang)
                        window.addEventListener('scroll', updateLineOnScroll, { passive: true });
                        
                        // Lắng nghe scroll trên container matching
                        var container = document.getElementById(containerId);
                        if (container) {
                            container.addEventListener('scroll', updateLineOnScroll, { passive: true });
                        }
                        
                        // Lắng nghe scroll trên exam-question-area (container chứa tất cả câu hỏi)
                        var questionArea = document.querySelector('.exam-question-area');
                        if (questionArea) {
                            questionArea.addEventListener('scroll', updateLineOnScroll, { passive: true });
                        }
                        
                        // Lắng nghe fullscreen change để cập nhật line khi chuyển fullscreen
                        var updateLineOnFullscreen = function() {
                            try {
                                // Đợi một chút để fullscreen hoàn tất
                                setTimeout(function() {
                                    if (line && line.position) {
                                        line.position();
                                    }
                                }, 100);
                            } catch (e) {
                                // Ignore errors
                            }
                        };
                        
                        // Lắng nghe fullscreen change events
                        document.addEventListener('fullscreenchange', updateLineOnFullscreen);
                        document.addEventListener('webkitfullscreenchange', updateLineOnFullscreen);
                        document.addEventListener('mozfullscreenchange', updateLineOnFullscreen);
                        document.addEventListener('MSFullscreenChange', updateLineOnFullscreen);
                        
                        // Lắng nghe resize để cập nhật khi fullscreen thay đổi kích thước
                        window.addEventListener('resize', updateLineOnFullscreen, { passive: true });
                        
                        // Lưu update function và elements để có thể remove sau
                        line._updateOnScroll = updateLineOnScroll;
                        line._updateOnFullscreen = updateLineOnFullscreen;
                        line._scrollElements = [window, container, questionArea].filter(function(el) { return el !== null && el !== undefined; });

                        // Lưu vào store
                        var store = __getStore(containerId);
                        store.push({ leftId: leftId, rightId: rightId, line: line });
                    } catch (e) {
                        console.error('Error creating leader line:', e, leftId, rightId);
                    }
                }, 50);
            } catch (e) {
                console.error('Error adding leader line:', e, leftId, rightId);
            }
        });
    },

    redrawLeaderLines: function(containerId) {
        var arr = __matchingLines[containerId];
        if (!arr) return;
        for (var i = 0; i < arr.length; i++) { 
            try { 
                if (arr[i] && arr[i].line && arr[i].line.position) {
                    arr[i].line.position(); 
                }
            } catch (e) {
                // Ignore errors
            }
        }
    },

    redrawAllLeaderLines: function() {
        // Redraw tất cả lines từ tất cả containers (dùng khi fullscreen change)
        for (var containerId in __matchingLines) {
            if (__matchingLines.hasOwnProperty(containerId)) {
                try {
                    window.matchingHelpers.redrawLeaderLines(containerId);
                } catch (e) {
                    // Ignore errors
                }
            }
        }
    },

    clearLeaderLines: function(containerId) {
        var arr = __matchingLines[containerId];
        if (!arr) return;
        for (var i = 0; i < arr.length; i++) { 
            try { 
                if (arr[i] && arr[i].line) {
                    // Remove scroll listeners
                    if (arr[i].line._updateOnScroll && arr[i].line._scrollElements) {
                        for (var j = 0; j < arr[i].line._scrollElements.length; j++) {
                            try {
                                arr[i].line._scrollElements[j].removeEventListener('scroll', arr[i].line._updateOnScroll);
                            } catch (e) {
                                // Ignore errors
                            }
                        }
                    }
                    // Remove fullscreen and resize listeners
                    if (arr[i].line._updateOnFullscreen) {
                        try {
                            document.removeEventListener('fullscreenchange', arr[i].line._updateOnFullscreen);
                            document.removeEventListener('webkitfullscreenchange', arr[i].line._updateOnFullscreen);
                            document.removeEventListener('mozfullscreenchange', arr[i].line._updateOnFullscreen);
                            document.removeEventListener('MSFullscreenChange', arr[i].line._updateOnFullscreen);
                            window.removeEventListener('resize', arr[i].line._updateOnFullscreen);
                        } catch (e) {
                            // Ignore errors
                        }
                    }
                    // Remove line
                    if (arr[i].line.remove) {
                        arr[i].line.remove(); 
                    }
                }
            } catch (e) {
                // Ignore errors
            }
        }
        delete __matchingLines[containerId];
    },

    removeLeaderLineByLeft: function(containerId, leftId) {
        var arr = __getStore(containerId);
        for (var i = arr.length - 1; i >= 0; i--) {
            if (arr[i] && arr[i].leftId === leftId) { 
                try { 
                    if (arr[i].line && arr[i].line.remove) {
                        arr[i].line.remove(); 
                    }
                } catch (e) {
                    // Ignore errors
                }
                arr.splice(i, 1); 
            }
        }
    },

    removeLeaderLineByRight: function(containerId, rightId) {
        var arr = __getStore(containerId);
        for (var i = arr.length - 1; i >= 0; i--) {
            if (arr[i] && arr[i].rightId === rightId) { 
                try { 
                    if (arr[i].line && arr[i].line.remove) {
                        arr[i].line.remove(); 
                    }
                } catch (e) {
                    // Ignore errors
                }
                arr.splice(i, 1); 
            }
        }
    },

    clearAllLeaderLines: function() {
        try {
            // Clear tất cả lines từ tất cả containers
            for (var containerId in __matchingLines) {
                if (__matchingLines.hasOwnProperty(containerId)) {
                    try {
                        window.matchingHelpers.clearLeaderLines(containerId);
                    } catch (e) {
                        // Ignore errors cho từng container
                    }
                }
            }
            // Reset store
            __matchingLines = {};
            
            // Xóa tất cả SVG của LeaderLine còn sót lại trong body
            try {
                var svgs = document.querySelectorAll('body > svg[data-leader-line], body > svg[data-container-id]');
                for (var i = 0; i < svgs.length; i++) {
                    try {
                        if (svgs[i].parentNode) {
                            svgs[i].parentNode.removeChild(svgs[i]);
                        }
                    } catch (e) {
                        // Ignore errors
                    }
                }
            } catch (e) {
                console.warn('Error cleaning up orphaned LeaderLine SVGs:', e);
            }
        } catch (e) {
            console.warn('Error clearing all leader lines:', e);
        }
    }
};

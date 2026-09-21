/* ==========================================================================
   TRIP ANALYZER - CLIENT JAVASCRIPT & GEOLOCATION & CHATBOT
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
    initGeolocation();
    initChatbot();
    initDistrictAutocomplete();
    initScrollAnimations();
    initHeroParallax();

    const heroIframe = document.getElementById('hero-3d-iframe');
    if (heroIframe) {
        heroIframe.addEventListener('load', () => onIframeLoad(heroIframe));
        if (heroIframe.contentDocument && heroIframe.contentDocument.readyState === 'complete') {
            onIframeLoad(heroIframe);
        }
    }
});

/* 1. Browser Geolocation API Integration */
function initGeolocation() {
    const geoBtn = document.getElementById('btn-use-geolocation');
    const sourceInput = document.getElementById('input-source');
    const latInput = document.getElementById('input-latitude');
    const lngInput = document.getElementById('input-longitude');

    if (geoBtn && sourceInput) {
        geoBtn.addEventListener('click', () => {
            if (!navigator.geolocation) {
                alert('Geolocation is not supported by your browser.');
                return;
            }

            geoBtn.disabled = true;
            geoBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span> Locating...';

            navigator.geolocation.getCurrentPosition(
                async (position) => {
                    const lat = position.coords.latitude;
                    const lng = position.coords.longitude;

                    if (latInput) latInput.value = lat;
                    if (lngInput) lngInput.value = lng;

                    try {
                        const res = await fetch(`/api/districts/nearest?lat=${encodeURIComponent(lat)}&lng=${encodeURIComponent(lng)}`);
                        if (res.ok) {
                            const data = await res.json();
                            if (data && data.name) {
                                sourceInput.value = data.name;
                            } else {
                                sourceInput.value = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
                            }
                        } else {
                            sourceInput.value = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
                        }
                    } catch {
                        sourceInput.value = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
                    }

                    geoBtn.disabled = false;
                    geoBtn.innerHTML = '<i class="bi me-1 bi-geo-alt-fill text-success"></i> Location Detected';
                    setTimeout(() => {
                        geoBtn.innerHTML = '<i class="bi me-1 bi-geo-alt"></i> Use My Current Location';
                    }, 3000);
                },
                (error) => {
                    geoBtn.disabled = false;
                    geoBtn.innerHTML = '<i class="bi me-1 bi-geo-alt"></i> Use My Current Location';
                    switch (error.code) {
                        case error.PERMISSION_DENIED:
                            alert('Location request denied. Please enter your location manually.');
                            break;
                        case error.POSITION_UNAVAILABLE:
                            alert('Location information is unavailable.');
                            break;
                        case error.TIMEOUT:
                            alert('The request to get user location timed out.');
                            break;
                        default:
                            alert('An unknown error occurred while fetching location.');
                            break;
                    }
                },
                { timeout: 10000, enableHighAccuracy: true }
            );
        });
    }
}

/* 2. Floating Chatbot Controller */
function initChatbot() {
    const triggerBtn = document.getElementById('chatbot-trigger');
    const windowEl = document.getElementById('chatbot-window');
    const closeBtn = document.getElementById('chatbot-close');
    const bodyEl = document.getElementById('chatbot-body');
    const inputEl = document.getElementById('chatbot-input');
    const sendBtn = document.getElementById('chatbot-send');

    if (!triggerBtn || !windowEl) return;

    let isSending = false;

    triggerBtn.addEventListener('click', () => {
        windowEl.classList.toggle('hidden');
        if (!windowEl.classList.contains('hidden') && bodyEl.children.length === 0) {
            fetchChatWelcomeMessage();
        }
    });

    if (closeBtn) {
        closeBtn.addEventListener('click', () => {
            windowEl.classList.add('hidden');
        });
    }

    if (sendBtn && inputEl) {
        sendBtn.addEventListener('click', (e) => {
            e.preventDefault();
            sendChatMessage();
        });

        inputEl.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                sendChatMessage();
            }
        });
    }

    async function sendChatMessage(customMsg) {
        const msg = (customMsg !== undefined ? customMsg : inputEl.value).trim();
        if (!msg || isSending) return;

        isSending = true;
        inputEl.value = "";

        appendUserMessage(msg);
        showTypingIndicator();

        try {
            const res = await fetch('/api/chatbot/ask', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: msg })
            });

            if (res.ok) {
                const data = await res.json();
                appendBotMessage(data);
            } else {
                appendBotMessage({ answer: 'Sorry, I am having trouble connecting right now. Please try again.' });
            }
        } catch (err) {
            appendBotMessage({ answer: 'Unable to reach the travel assistant service.' });
        } finally {
            hideTypingIndicator();
            isSending = false;
            inputEl.focus();
        }
    }

    async function fetchChatWelcomeMessage() {
        showTypingIndicator();
        try {
            const res = await fetch('/api/chatbot/ask', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: '' })
            });
            if (res.ok) {
                const data = await res.json();
                appendBotMessage(data);
            }
        } catch (err) {
            appendBotMessage({ answer: 'Hello! I am your Trip Assistant. How can I help you optimize your travel today?' });
        } finally {
            hideTypingIndicator();
        }
    }

    function appendUserMessage(msg) {
        const userBubble = document.createElement('div');
        userBubble.className = 'chat-bubble user';
        userBubble.textContent = msg;
        bodyEl.appendChild(userBubble);
        scrollToBottom();
    }

    function appendBotMessage(response) {
        const botBubble = document.createElement('div');
        botBubble.className = 'chat-bubble bot';

        const answerText = response.answer || response.Answer || response.message || response.Message || response.response || response.Response || '';
        let html = `<div>${escapeHtml(answerText)}</div>`;

        const suggestions = response.suggestedQuestions || response.SuggestedQuestions || [];
        if (suggestions && suggestions.length > 0) {
            html += `<div class="mt-2 d-flex flex-row flex-wrap gap-1 suggestions-container">`;
            suggestions.forEach(q => {
                html += `<button type="button" class="chip-btn" onclick="sendSuggestedQuestion('${escapeJs(q)}')">${escapeHtml(q)}</button>`;
            });
            html += `</div>`;
        }

        botBubble.innerHTML = html;
        bodyEl.appendChild(botBubble);
        scrollToBottom();
    }

    function showTypingIndicator() {
        hideTypingIndicator();
        const typingBubble = document.createElement('div');
        typingBubble.className = 'chat-bubble bot typing-indicator-bubble';
        typingBubble.id = 'chatbot-typing-indicator';
        typingBubble.innerHTML = `
            <div class="typing-dots">
                <span></span>
                <span></span>
                <span></span>
            </div>`;
        bodyEl.appendChild(typingBubble);
        scrollToBottom();
    }

    function hideTypingIndicator() {
        const existing = document.getElementById('chatbot-typing-indicator');
        if (existing) {
            existing.remove();
        }
    }

    function scrollToBottom() {
        bodyEl.scrollTop = bodyEl.scrollHeight;
    }

    window.sendSuggestedQuestion = function (q) {
        sendChatMessage(q);
    };
}

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function escapeJs(str) {
    if (!str) return '';
    return str.replace(/'/g, "\\'");
}

/* Quick Route Selector Helper for Search Hero */
function setQuickRoute(source, destination) {
    const srcInput = document.getElementById('input-source');
    const dstInput = document.getElementById('input-destination');
    if (srcInput && dstInput) {
        srcInput.value = source;
        dstInput.value = destination;
    }
}

/* 3. District Autocomplete Integration */
function initDistrictAutocomplete() {
    setupAutocomplete('input-source', 'autocomplete-results-source');
    setupAutocomplete('input-destination', 'autocomplete-results-dest');
}

function setupAutocomplete(inputId, resultsId) {
    const input = document.getElementById(inputId);
    if (!input) return;

    let resultsDiv = document.getElementById(resultsId);
    if (!resultsDiv) {
        // Create results container if it doesn't exist
        resultsDiv = document.createElement('div');
        resultsDiv.id = resultsId;
        resultsDiv.className = 'autocomplete-results list-group position-absolute w-100 shadow-sm';
        resultsDiv.style.zIndex = '1050';
        resultsDiv.style.display = 'none';

        // Make parent relative
        if (input.parentElement) {
            input.parentElement.style.position = 'relative';
            input.parentElement.appendChild(resultsDiv);
        }
    }

    let timeout = null;

    input.addEventListener('input', function () {
        clearTimeout(timeout);
        const query = this.value.trim();

        if (query.length < 2) {
            resultsDiv.style.display = 'none';
            return;
        }

        timeout = setTimeout(async () => {
            try {
                const response = await fetch(`/api/districts/search?q=${encodeURIComponent(query)}`);
                if (!response.ok) throw new Error('Network response was not ok');
                const data = await response.json();

                resultsDiv.innerHTML = '';
                if (data && data.length > 0) {
                    data.forEach(item => {
                        const div = document.createElement('button');
                        div.type = 'button';
                        div.className = 'list-group-item list-group-item-action autocomplete-item';
                        div.innerHTML = `<i class="bi bi-geo-alt text-muted me-2"></i> ${escapeHtml(item.displayName)}`;
                        div.onclick = function () {
                            input.value = item.name;
                            resultsDiv.style.display = 'none';
                        };
                        resultsDiv.appendChild(div);
                    });
                    resultsDiv.style.display = 'block';
                } else {
                    resultsDiv.style.display = 'none';
                }
            } catch (error) {
                console.error("Error fetching districts", error);
                resultsDiv.style.display = 'none';
            }
        }, 300);
    });

    // Hide autocomplete when clicking outside
    document.addEventListener('click', function (e) {
        if (e.target !== input && e.target !== resultsDiv) {
            resultsDiv.style.display = 'none';
        }
    });
}

/* 4. ThreeUI Iframe Isolation Helper */
function onIframeLoad(iframe) {
    try {
        const frameDoc = iframe.contentDocument || iframe.contentWindow.document;
        if (!frameDoc) return;

        // Find the canvas inside the iframe (SylvaHero canvas is #scene)
        const canvas = frameDoc.querySelector('#scene');
        if (!canvas) return;

        // Isolate canvas using ThreeUI canonical data attributes
        canvas.setAttribute("data-threeui-background-layer", "");
        canvas.setAttribute("data-threeui-background-fill", "");

        frameDoc.documentElement.setAttribute("data-threeui-presentation", "background");

        // Add style to iframe to hide rest of document and make background transparent
        const presentationStyle = frameDoc.createElement("style");
        presentationStyle.id = "threeui-background-presentation";
        presentationStyle.textContent = `
            html[data-threeui-presentation="background"],
            html[data-threeui-presentation="background"] body {
                width: 100% !important;
                height: 100% !important;
                min-height: 100% !important;
                overflow: hidden !important;
                background: transparent !important;
            }
            html[data-threeui-presentation="background"] body * {
                visibility: hidden !important;
                pointer-events: none !important;
            }
            html[data-threeui-presentation="background"] [data-threeui-background-layer],
            html[data-threeui-presentation="background"] [data-threeui-background-layer] * {
                visibility: visible !important;
            }
            html[data-threeui-presentation="background"] [data-threeui-background-fill] {
                position: fixed !important;
                inset: 0 !important;
                width: 100vw !important;
                height: 100vh !important;
                max-width: none !important;
                max-height: none !important;
                margin: 0 !important;
                transform: none !important;
            }
        `;
        frameDoc.head.appendChild(presentationStyle);

        // Retrigger resize within iframe content window
        iframe.contentWindow.requestAnimationFrame(() => {
            iframe.contentWindow.dispatchEvent(new Event("resize"));
        });

        // Fade in iframe
        iframe.style.opacity = "1";
    } catch (e) {
        console.error("ThreeUI iframe isolation failed:", e);
    }
}

/* 5. Parallax, Swap, and Scroll Trigger Helpers */
function swapLocations() {
    const srcInput = document.getElementById('input-source');
    const dstInput = document.getElementById('input-destination');
    const btnSwap = document.getElementById('btn-swap');
    if (srcInput && dstInput) {
        const temp = srcInput.value;
        srcInput.value = dstInput.value;
        dstInput.value = temp;
        if (btnSwap) {
            btnSwap.style.transform = btnSwap.style.transform === 'rotate(180deg)' ? 'rotate(0deg)' : 'rotate(180deg)';
        }
    }
}

function initScrollAnimations() {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                if (entry.target.id === 'stats-row') {
                    const counters = entry.target.querySelectorAll('.count-number');
                    counters.forEach(counter => {
                        const target = +counter.getAttribute('data-target');
                        let count = 0;
                        const speed = Math.max(1, target / 50);
                        const updateCount = () => {
                            count += speed;
                            if (count < target) {
                                counter.textContent = Math.floor(count);
                                setTimeout(updateCount, 20);
                            } else {
                                counter.textContent = target;
                            }
                        };
                        updateCount();
                    });
                }
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    document.querySelectorAll('.show-on-scroll, #stats-row').forEach(el => {
        observer.observe(el);
    });
}

function initHeroParallax() {
    const heroOverlay = document.getElementById('hero-overlay-content');
    const heroWrapper = document.querySelector('.hero-wrapper');
    if (!heroOverlay || !heroWrapper) return;

    heroWrapper.addEventListener('mousemove', (e) => {
        if (window.innerWidth < 768) return; // Disable parallax on mobile
        const rect = heroWrapper.getBoundingClientRect();
        const x = e.clientX - rect.left - rect.width / 2;
        const y = e.clientY - rect.top - rect.height / 2;

        const tiltX = (y / rect.height) * 20;
        const tiltY = -(x / rect.width) * 20;

        heroOverlay.style.transform = `translate3d(${tiltY}px, ${tiltX}px, 0)`;
        heroOverlay.style.transition = 'transform 0.1s ease-out';
    });

    heroWrapper.addEventListener('mouseleave', () => {
        heroOverlay.style.transform = 'translate3d(0px, 0px, 0)';
        heroOverlay.style.transition = 'transform 0.5s ease-out';
    });
}

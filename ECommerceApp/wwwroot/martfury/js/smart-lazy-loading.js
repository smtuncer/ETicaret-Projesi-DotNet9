/**
 * Smart Lazy Loading for Images
 * Modern ES6+ implementation using IntersectionObserver
 * Features: Progressive loading, blur effect, error handling, performance optimization
 */

class SmartLazyLoader {
    constructor(options = {}) {
        this.config = {
            // Root margin for early loading (load before element enters viewport)
            rootMargin: '50px 0px',
            
            // Threshold for intersection
            threshold: 0.1,
            
            // Blur effect settings
            enableBlur: true,
            blurAmount: 10,
            
            // Progressive loading
            enableProgressive: true,
            
            // Error handling
            enableErrorHandling: true,
            fallbackImage: 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="400" height="300"%3E%3Crect width="400" height="300" fill="%23f0f0f0"/%3E%3Ctext x="50%" y="50%" text-anchor="middle" dy=".3em" fill="%23999"%3EImage not available%3C/text%3E%3C/svg%3E',
            
            // Performance settings
            enableWebP: true,
            enableRetina: true,
            
            // Animation settings
            fadeInDuration: 300,
            
            ...options
        };
        
        this.observer = null;
        this.images = new Map();
        this.loadedImages = new Set();
        
        this.init();
    }

    init() {
        // Check for IntersectionObserver support
        if (!('IntersectionObserver' in window)) {
            console.warn('IntersectionObserver not supported, falling back to immediate loading');
            this.fallbackLoad();
            return;
        }

        this.createObserver();
        this.findImages();
        this.addStyles();
        
        console.log('🖼️ Smart Lazy Loading initialized:', this.images.size, 'images found');
    }

    createObserver() {
        this.observer = new IntersectionObserver(
            (entries) => this.handleIntersection(entries),
            {
                rootMargin: this.config.rootMargin,
                threshold: this.config.threshold
            }
        );
    }

    findImages() {
        // Find all images that should be lazy loaded
        const imageElements = document.querySelectorAll('img[data-src], img[data-lazy], img[loading="lazy"]');
        
        imageElements.forEach(img => {
            this.setupImage(img);
        });

        // Also find background images with data-background
        const bgElements = document.querySelectorAll('[data-background]');
        bgElements.forEach(el => {
            this.setupBackgroundImage(el);
        });
    }

    setupImage(img) {
        const imageData = {
            element: img,
            type: 'img',
            originalSrc: img.dataset.src || img.dataset.lazy || img.src,
            srcset: img.dataset.srcset,
            sizes: img.dataset.sizes,
            alt: img.alt || 'Image',
            isLoaded: false
        };

        // Generate placeholder if needed
        if (!img.src || img.src === imageData.originalSrc) {
            img.src = this.generatePlaceholder(img);
        }

        // Add blur effect if enabled
        if (this.config.enableBlur) {
            img.style.filter = `blur(${this.config.blurAmount}px)`;
            img.style.transition = `filter ${this.config.fadeInDuration}ms ease`;
        }

        // Add loading class
        img.classList.add('lazy-loading');
        
        // Store image data and start observing
        this.images.set(img, imageData);
        this.observer.observe(img);
    }

    setupBackgroundImage(element) {
        const imageData = {
            element: element,
            type: 'background',
            originalSrc: element.dataset.background,
            isLoaded: false
        };

        // Add blur effect if enabled
        if (this.config.enableBlur) {
            element.style.filter = `blur(${this.config.blurAmount}px)`;
            element.style.transition = `filter ${this.config.fadeInDuration}ms ease`;
        }

        // Add loading class
        element.classList.add('lazy-loading');

        this.images.set(element, imageData);
        this.observer.observe(element);
    }

    handleIntersection(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const element = entry.target;
                const imageData = this.images.get(element);
                
                if (imageData && !imageData.isLoaded) {
                    this.loadImage(element, imageData);
                }
            }
        });
    }

    async loadImage(element, imageData) {
        try {
            imageData.isLoaded = true;
            
            // Stop observing this element
            this.observer.unobserve(element);
            
            if (imageData.type === 'img') {
                await this.loadRegularImage(element, imageData);
            } else if (imageData.type === 'background') {
                await this.loadBackgroundImage(element, imageData);
            }
            
            this.onImageLoaded(element, imageData);
            
        } catch (error) {
            this.onImageError(element, imageData, error);
        }
    }

    loadRegularImage(img, imageData) {
        return new Promise((resolve, reject) => {
            const tempImg = new Image();
            
            // Set up error handling
            tempImg.onerror = () => reject(new Error('Failed to load image'));
            
            tempImg.onload = () => {
                // Apply the loaded image
                if (imageData.srcset) {
                    img.srcset = imageData.srcset;
                }
                if (imageData.sizes) {
                    img.sizes = imageData.sizes;
                }
                
                img.src = this.getOptimizedSrc(imageData.originalSrc);
                resolve();
            };

            // Start loading
            tempImg.src = this.getOptimizedSrc(imageData.originalSrc);
        });
    }

    loadBackgroundImage(element, imageData) {
        return new Promise((resolve, reject) => {
            const tempImg = new Image();
            
            tempImg.onerror = () => reject(new Error('Failed to load background image'));
            
            tempImg.onload = () => {
                const optimizedSrc = this.getOptimizedSrc(imageData.originalSrc);
                element.style.backgroundImage = `url(${optimizedSrc})`;
                resolve();
            };

            tempImg.src = this.getOptimizedSrc(imageData.originalSrc);
        });
    }

    getOptimizedSrc(originalSrc) {
        if (!originalSrc) return originalSrc;

        let optimizedSrc = originalSrc;

        // WebP support detection and conversion
        if (this.config.enableWebP && this.supportsWebP()) {
            optimizedSrc = this.convertToWebP(originalSrc);
        }

        // Retina support
        if (this.config.enableRetina && this.isRetina()) {
            optimizedSrc = this.getRetinaSrc(optimizedSrc);
        }

        return optimizedSrc;
    }

    convertToWebP(src) {
        // Simple WebP conversion (you might want to implement server-side conversion)
        if (src.match(/\.(jpg|jpeg|png)$/i) && !src.includes('.webp')) {
            return src.replace(/\.(jpg|jpeg|png)$/i, '.webp');
        }
        return src;
    }

    getRetinaSrc(src) {
        // Add @2x for retina images
        return src.replace(/(\.[^.]+)$/, '@2x$1');
    }

    supportsWebP() {
        // Check WebP support
        if (!this._webpSupport) {
            const canvas = document.createElement('canvas');
            canvas.width = canvas.height = 1;
            this._webpSupport = canvas.toDataURL('image/webp').indexOf('webp') > -1;
        }
        return this._webpSupport;
    }

    isRetina() {
        return window.devicePixelRatio > 1;
    }

    onImageLoaded(element, imageData) {
        // Remove blur effect
        if (this.config.enableBlur) {
            element.style.filter = 'none';
        }

        // Update classes
        element.classList.remove('lazy-loading');
        element.classList.add('lazy-loaded');

        // Add to loaded images set
        this.loadedImages.add(element);

        // Trigger custom event
        element.dispatchEvent(new CustomEvent('lazyLoaded', {
            detail: { imageData, loader: this }
        }));

        console.log('✅ Image loaded:', imageData.originalSrc);
    }

    onImageError(element, imageData, error) {
        console.error('❌ Failed to load image:', imageData.originalSrc, error);

        if (this.config.enableErrorHandling) {
            if (imageData.type === 'img') {
                element.src = this.config.fallbackImage;
                element.alt = 'Failed to load image';
            } else if (imageData.type === 'background') {
                element.style.backgroundImage = `url(${this.config.fallbackImage})`;
            }

            // Remove blur effect even on error
            if (this.config.enableBlur) {
                element.style.filter = 'none';
            }

            element.classList.remove('lazy-loading');
            element.classList.add('lazy-error');

            // Trigger error event
            element.dispatchEvent(new CustomEvent('lazyError', {
                detail: { imageData, error, loader: this }
            }));
        }
    }

    generatePlaceholder(img) {
        // Generate a simple SVG placeholder based on image dimensions
        const width = img.getAttribute('width') || 400;
        const height = img.getAttribute('height') || 300;
        
        return `data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}"%3E%3Crect width="${width}" height="${height}" fill="%23f0f0f0"/%3E%3Ctext x="50%" y="50%" text-anchor="middle" dy=".3em" fill="%23ccc" font-size="14"%3ELoading...%3C/text%3E%3C/svg%3E`;
    }

    addStyles() {
        const styles = `
            .lazy-loading {
                background-color: #f0f0f0;
                transition: opacity ${this.config.fadeInDuration}ms ease;
            }
            
            .lazy-loaded {
                animation: lazy-fade-in ${this.config.fadeInDuration}ms ease;
            }
            
            .lazy-error {
                background-color: #ffe6e6;
                border: 1px solid #ffcccc;
            }
            
            @keyframes lazy-fade-in {
                from {
                    opacity: 0;
                }
                to {
                    opacity: 1;
                }
            }
            
            /* Progressive enhancement for images */
            img[data-src]:not(.lazy-loaded) {
                opacity: 0.7;
            }
            
            img.lazy-loaded {
                opacity: 1;
            }
        `;

        const styleSheet = document.createElement('style');
        styleSheet.textContent = styles;
        document.head.appendChild(styleSheet);
    }

    // Fallback for browsers without IntersectionObserver
    fallbackLoad() {
        const images = document.querySelectorAll('img[data-src], img[data-lazy]');
        const bgElements = document.querySelectorAll('[data-background]');

        images.forEach(img => {
            const src = img.dataset.src || img.dataset.lazy;
            if (src) {
                img.src = src;
                img.classList.add('lazy-loaded');
            }
        });

        bgElements.forEach(el => {
            const bgSrc = el.dataset.background;
            if (bgSrc) {
                el.style.backgroundImage = `url(${bgSrc})`;
                el.classList.add('lazy-loaded');
            }
        });
    }

    // Public methods
    loadAll() {
        this.images.forEach((imageData, element) => {
            if (!imageData.isLoaded) {
                this.loadImage(element, imageData);
            }
        });
    }

    refresh() {
        this.findImages();
    }

    destroy() {
        if (this.observer) {
            this.observer.disconnect();
        }
        this.images.clear();
        this.loadedImages.clear();
    }

    // Statistics
    getStats() {
        return {
            totalImages: this.images.size,
            loadedImages: this.loadedImages.size,
            pendingImages: this.images.size - this.loadedImages.size,
            loadedPercentage: Math.round((this.loadedImages.size / this.images.size) * 100)
        };
    }
}

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.lazyLoader = new SmartLazyLoader();
    });
} else {
    window.lazyLoader = new SmartLazyLoader();
}

// Export for manual use
window.SmartLazyLoader = SmartLazyLoader;

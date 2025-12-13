function loadCartPreview() {
    $.get('/sepet/ozet', function (data) {
        $('#cart-preview-container').html(data);
        $('#cart-preview-container-mobile').html(data);
        $('#cart-preview-container-sidebar').html(data);

        // Update count
        var count = $(data).find('.ps-product--cart-mobile').length;
        $('#cart-count i').text(count);
        $('#cart-count-mobile i').text(count);
        $('#mobile-bottom-cart-count').text(count);
    }).fail(function (xhr) {
        // If user is not logged in (401), silently fail
        // Don't show error message on page load
        if (xhr.status !== 401) {
            console.error('Sepet yüklenirken hata oluştu:', xhr.status);
        }
    });
}

function addToCart(productId, quantity) {
    $.post('/sepet/ekle', { productId: productId, quantity: quantity }, function (response) {
        if (response.success) {
            loadCartPreview();
            // Show success notification (NToastNotify will handle this from server)
            location.reload(); // Reload to show toast notification
        } else {
            alert(response.message);
        }
    }).fail(function (xhr) {
        if (xhr.status === 401) {
            // Unauthorized - User not logged in
            // Redirect to login with return URL
            var currentUrl = encodeURIComponent(window.location.pathname + window.location.search);
            window.location.href = '/giris?returnUrl=' + currentUrl;
        } else {
            alert('Bir hata oluştu. Lütfen tekrar deneyiniz.');
        }
    });
}

function removeFromCartHeader(itemId) {
    $.post('/sepet/sil', { itemId: itemId }, function (response) {
        if (response.success) {
            loadCartPreview();
            // If we are on the cart page, reload the page
            if (window.location.pathname === '/sepet') {
                location.reload();
            }
        } else {
            alert(response.message);
        }
    });
}

function updateQuantity(itemId, quantity) {
    $.post('/sepet/guncelle', { itemId: itemId, quantity: quantity }, function (response) {
        if (response.success) {
            location.reload();
        } else {
            alert(response.message);
        }
    });
}

function removeFromCart(itemId) {
    if (confirm('Bu ürünü sepetten silmek istediğinize emin misiniz?')) {
        $.post('/sepet/sil', { itemId: itemId }, function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        });
    }
}

function applyCoupon() {
    var code = $('#couponCode').val();
    if (!code) {
        alert('Lütfen bir kupon kodu giriniz.');
        return;
    }

    $.post('/sepet/kupon-uygula', { code: code }, function (response) {
        if (response.success) {
            location.reload();
        } else {
            alert(response.message);
        }
    }).fail(function (xhr, status, error) {
        console.error("Kupon uygulama hatası:", error);
        alert('Kupon uygulanırken bir hata oluştu. Lütfen tekrar deneyiniz.');
    });
}

function removeCoupon() {
    if (confirm('Kuponu kaldırmak istediğinize emin misiniz?')) {
        $.post('/sepet/kupon-kaldir', function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        });
    }
}

$(document).ready(function () {
    loadCartPreview();

    // Mobile Bottom Navigation - Search Toggle
    $('#mobile-search-toggle').on('click', function (e) {
        e.preventDefault();
        // Focus on the mobile search input
        $('.ps-search--mobile .form-control').focus();
        // Scroll to search bar
        $('html, body').animate({
            scrollTop: $('.ps-search--mobile').offset().top - 20
        }, 300);
    });

    // Cart Page Quantity Update
    $(document).on('click', '.js-qty-update', function (e) {
        e.preventDefault();
        e.stopPropagation(); // Stop bubbling to prevent theme JS interference

        var btn = $(this);
        var itemId = btn.data('id');
        var action = btn.data('action');
        var input = $('#qty-' + itemId);

        // If input not found by ID (fallback), try finding sibling
        if (input.length === 0) {
            input = btn.siblings('input');
        }

        var currentQty = parseInt(input.val()) || 1;

        var newQty = currentQty;
        if (action === 'increase') {
            newQty++;
        } else if (action === 'decrease') {
            newQty--;
        }

        if (newQty < 1) return;

        // Optimistic update for visual feedback
        input.val(newQty);

        updateQuantity(itemId, newQty);
    });
});

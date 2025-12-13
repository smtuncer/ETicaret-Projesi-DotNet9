function toggleFavorite(productId, element) {
    var btn = $(element);

    // Toast definition
    const Toast = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.addEventListener('mouseenter', Swal.stopTimer)
            toast.addEventListener('mouseleave', Swal.resumeTimer)
        }
    });

    $.ajax({
        url: '/api/favorites/toggle',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(productId),
        success: function (response) {
            if (response.success) {
                if (response.isFav) {
                    btn.addClass('active');
                    Toast.fire({
                        icon: 'success',
                        title: 'Ürün favorilere eklendi'
                    });
                } else {
                    btn.removeClass('active');
                    Toast.fire({
                        icon: 'info',
                        title: 'Ürün favorilerden çıkarıldı'
                    });

                    // If on favorites page, remove the item with animation
                    if (window.location.pathname === '/favorilerim') {
                        var col = btn.closest('.col-xl-4, .col-xl-3'); // Match both layouts
                        col.fadeOut(300, function () {
                            $(this).remove();
                            if ($('.ps-product').length === 0) {
                                location.reload();
                            }
                        });
                    }
                }
            } else {
                Toast.fire({
                    icon: 'error',
                    title: 'İşlem başarısız'
                });
            }
        },
        error: function (xhr) {
            if (xhr.status === 401) {
                var currentUrl = encodeURIComponent(window.location.pathname + window.location.search);
                window.location.href = '/giris?returnUrl=' + currentUrl;
            } else {
                console.error(xhr);
                Toast.fire({
                    icon: 'error',
                    title: 'Bir hata oluştu. Lütfen giriş yaptığınızdan emin olun.'
                });
            }
        }
    });
}

$(document).ready(function () {
    // Load favorites on page load to highlight icons
    $.get('/api/favorites/list', function (ids) {
        if (Array.isArray(ids)) {
            ids.forEach(function (id) {
                // Find buttons for this product and add active class
                $('.toggle-favorite[data-id="' + id + '"]').addClass('active');
                // Also try generic finding if data-id is missing but onclick has it
                $('a[onclick*="toggleFavorite(' + id + ',"]').addClass('active');
            });
        }
    });
});

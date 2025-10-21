$(document).ready(function () {
    $('.datatable').DataTable({
        "columnDefs": [{
            "targets": 'no-sort',
            "orderable": false,
        }],
        "stateSave": true,
        "stateDuration": 0
    });
});
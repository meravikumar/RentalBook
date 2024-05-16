$('#btnAddReason').click(function () {
    $('#ReasonModal').modal('show');
})

function AddReason(Reason) {
    $.ajax({
        url: '/Admin/User/Reject',
        type: 'Post',
        data: { 'Username': $("#btnAddReason").val(), 'Reason': $("#Reason").val() },
        success: function (data) {
            console.log(data);
            $('body').html(data);
            ClearTextBox();
            HideModelPopUp();
            window.location.reload();
        },
        error: function () {
            alert("Give the Reason!");
        }
    });
    function HideModelPopUp() {
        $('#ReasonModal').modal('hide');
    }
    function ClearTextBox() {
        $('#Reason').val('');
    }
}


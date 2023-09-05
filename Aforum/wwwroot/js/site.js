document.addEventListener("DOMContentLoaded", () => {
    const signInButton = document.getElementById("signin-button");
    if (signInButton) signInButton.addEventListener('click', signInButtonClick);

 

});

function signInButtonClick() {
    const userLoginInput = document.getElementById("signin-login");
    const userPasswordInput = document.getElementById("signin-password");
    if (!userLoginInput) throw "Елемент не знайдено: signin-login";
    if (!userPasswordInput) throw "Елемент не знайдено: signin-password";

    const userLogin = userLoginInput.value;
    const userPassword = userPasswordInput.value;
    if (userLogin.length === 0) {
        alert("Введіть логін");
        return;
    }
    if (userPassword.length === 0) {
        alert("Введіть пароль");
        return;
    }
    // console.log(userLogin, userPassword);
    const data = new FormData();
    data.append("login", userLogin);
    data.append("password", userPassword);

    const modalBody = document.getElementById("signinModal").querySelector(".modal-body"); // Отримуємо елемент модального вікна

    const message = document.createElement("p"); // Створюємо елемент для відображення надпису
    message.textContent = "Зачекайте, триває авторизація..."; // Задаємо текст надпису
    modalBody.appendChild(message); // Додаємо надпис до модального вікна

    
    fetch(                      
        "/User/LogIn",         
        {                       
            method: "POST",     
            body: data          
        })                      
        .then(r => r.json())    
        .then(j => {            -             
            modalBody.removeChild(message); 
            if (typeof j.status != 'undefined') {
                if (j.status == 'OK') {
                    window.location.reload();   
                }
                else {
                    const error = document.createElement("p"); 
                    error.textContent = "Неправильний логін або пароль"; 
                    error.style.color = "red"; 
                    modalBody.appendChild(error); 
                }
            }
        });

}

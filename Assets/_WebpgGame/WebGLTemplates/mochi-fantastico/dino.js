
//board
let board;
let boardWidth = 750;
let boardHeight = 250;
let context;

//dino
let dinoWidth = 88;
let dinoHeight = 94;
let dinoX = 50;
let dinoY = boardHeight - dinoHeight;
let dinoImg;

let dino = {
    x : dinoX,
    y : dinoY,
    width : dinoWidth,
    height : dinoHeight
}

//cactus
let cactusArray = [];

let cactus1Width = 34;
let cactus2Width = 69;
let cactus3Width = 102;

let cactusHeight = 70;
let cactusX = 700;
let cactusY = boardHeight - cactusHeight;

let cactus1Img;
let cactus2Img;
let cactus3Img;

//physics
let velocityX = -6; //cactus moving left speed
let velocityY = 0;
let gravity = .4;

let gameOver = false;
let score = 0;

// Variables para la animación del dinosaurio
let dinoFrames = [];  // Array para las imágenes de los frames del dinosaurio
let currentFrame = 0;  // El índice del frame actual
let frameSpeed = 10;   // Controla la velocidad de la animación (cuántos updates por frame)
let frameCounter = 0;  // Contador para la velocidad de cambio de frame

// Cargar las imágenes de los frames de la animación del dinosaurio
dinoFrames[0] = new Image();
dinoFrames[0].src = './img/dino-run1.png';

dinoFrames[1] = new Image();
dinoFrames[1].src = './img/dino-run2.png';

function moveDino(e) {
    if (gameOver) {
        return;
    }

    console.log("try to jump");

    if (dino.y == dinoY) {
        //jump
        velocityY = -13;
    }
}

window.onload = function() {
    board = document.getElementById("board");
    board.height = boardHeight;
    board.width = boardWidth;

    context = board.getContext("2d"); //used for drawing on the board

    //draw initial dinosaur
    // context.fillStyle="green";
    // context.fillRect(dino.x, dino.y, dino.width, dino.height);

    dinoImg = new Image();
    dinoImg.src = "./img/dino.png";
    dinoImg.onload = function() {
        context.drawImage(dinoImg, dino.x, dino.y, dino.width, dino.height);
    }

    cactus1Img = new Image();
    cactus1Img.src = "./img/big-cactus1.png";

    cactus2Img = new Image();
    cactus2Img.src = "./img/big-cactus2.png";

    cactus3Img = new Image();
    cactus3Img.src = "./img/big-cactus3.png";

    requestAnimationFrame(update);
    setInterval(placeCactus, 1000); //1000 milliseconds = 1 second

    // Obtener el enlace por su ID
    const dinoButton = document.getElementById("dinoButton");

    // Agregar un event listener para el clic
    dinoButton.addEventListener("click", function(event) {
        event.preventDefault();
        if(gameOver)
        {
            cactusArray = [];
            gameOver = false;
            score = 0;
        }
        else
        {
            moveDino(event);
        }
    });
    //document.addEventListener("touchstart", moveDino);
}

function update() {
    requestAnimationFrame(update);
    if (gameOver) {
        return;
    }
    context.clearRect(0, 0, board.width, board.height);

    // Aplicar gravedad
    velocityY += gravity;
    dino.y = Math.min(dino.y + velocityY, dinoY); // Asegurarse de que no exceda el suelo

    // Animación del dinosaurio
    frameCounter++;
    if (frameCounter >= frameSpeed) {
        frameCounter = 0;
        currentFrame = (currentFrame + 1) % dinoFrames.length; // Cambiar al siguiente frame
    }

    // Dibuja el dino con el frame actual
    context.drawImage(dinoFrames[currentFrame], dino.x, dino.y, dino.width, dino.height);

    // Cactus
    for (let i = 0; i < cactusArray.length; i++) {
        let cactus = cactusArray[i];
        cactus.x += velocityX;
        context.drawImage(cactus.img, cactus.x, cactus.y, cactus.width, cactus.height);

        if (detectCollision(dino, cactus)) {
            gameOver = true;
            dinoImg.src = "./img/dino-dead.png";
            dinoImg.onload = function() {
                context.clearRect(0, 0, board.width, board.height);
                context.drawImage(dinoImg, dino.x, dino.y, dino.width, dino.height);
            }
        }
    }

    // Score
    context.fillStyle = "white";
    context.font = "20px courier";
    score++;
    context.fillText(score, 5, 20);
}



function placeCactus() {
    if (gameOver) {
        return;
    }

    //place cactus
    let cactus = {
        img : null,
        x : cactusX,
        y : cactusY,
        width : null,
        height: cactusHeight
    }

    let placeCactusChance = Math.random(); //0 - 0.9999...

    if (placeCactusChance > .90) { //10% you get cactus3
        cactus.img = cactus3Img;
        cactus.width = cactus3Width;
        cactusArray.push(cactus);
    }
    else if (placeCactusChance > .70) { //30% you get cactus2
        cactus.img = cactus2Img;
        cactus.width = cactus2Width;
        cactusArray.push(cactus);
    }
    else if (placeCactusChance > .50) { //50% you get cactus1
        cactus.img = cactus1Img;
        cactus.width = cactus1Width;
        cactusArray.push(cactus);
    }

    if (cactusArray.length > 5) {
        cactusArray.shift(); //remove the first element from the array so that the array doesn't constantly grow
    }
}

function detectCollision(a, b) {
    return a.x < b.x + b.width &&   //a's top left corner doesn't reach b's top right corner
           a.x + a.width > b.x &&   //a's top right corner passes b's top left corner
           a.y < b.y + b.height &&  //a's top left corner doesn't reach b's bottom left corner
           a.y + a.height > b.y;    //a's bottom left corner passes b's top left corner
}
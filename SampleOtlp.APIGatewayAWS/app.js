const express = require('express');
const lambdaHandler = require('./lambda-handler');

const PORT = parseInt(process.env.PORT || '8899');
const app = express();

function getRandomNumber(min, max) {
    return Math.floor(Math.random() * (max - min) + min);
}

app.get('/rolldice', (req, res) => {
    // res.send(getRandomNumber(1, 6).toString());

    lambdaHandler.handler({ userName: "0842480289218" });
    res.send('OK');
});

app.listen(PORT, () => {
    console.log(`Listening for requests on http://localhost:${PORT}`);
});
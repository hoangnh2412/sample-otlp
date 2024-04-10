const https = require('https');
const axios = require('axios');
require('dotenv').config();

const DOMAIN_USER_SERVICE = process.env.DOMAIN_USER_SERVICE || '';
const API_KEY = process.env.API_KEY || '';

// async function run(event) {
exports.handler = async (event) => {
    console.log("EVENT", event);
    const res = await getUserInfo(event.userName)
    console.log("RESPONES DATA: ", res);
    if (res.success === true) {
        const userInfo = {
            Id: res.data.id,
            UserName: res.data.userName,
            FirstName: res.data.firstName,
            LastName: res.data.lastName,
            Email: res.data.email,
            PhoneNumber: res.data.phoneNumber,
        };

        event.response = {
            "claimsOverrideDetails": {
                "claimsToAddOrOverride": {
                    "UserInfo": JSON.stringify(userInfo)
                }
            }
        };
    }
    else {
        console.log("Failed to retrieve API data");
    }
};

function getUserInfo(username) {
    return new Promise((resolve, reject) => {
        const url = `${DOMAIN_USER_SERVICE}/api/v2/m2mdata/user/${username}/info`;
        // console.log("URL: ", url);

        axios
            .get(url, {
                headers: {
                    'X-API-KEY': API_KEY,
                    'belletorus-api-key': API_KEY
                },
            })
            .then(response => {
                // console.log(response.data);
                resolve(response.data);
            })
            .catch(error => {
                console.log(error);
                reject(error);
            });
    });
};

// run({ userName: '0842480289218' });
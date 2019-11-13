export class RequestObject {
    command ="getKey";
    payload = null;
    target = "";
    ipAddress = ""
    user = "";
    password = "";
    expectedAuthKey = ""
    constructor(name, payload) {
      this.command = name;
      this.payload = JSON.stringify(payload);
    }
  }

  export class ResponseWrapper {
    currentPublicKey="";
    authKey= "";
    cipherV2Bytes= "";
  };
  export class ResponsePayload {
    responsePayload = { };
    status = 0;
    statusText="";
    authKey = ""
  }
  
  
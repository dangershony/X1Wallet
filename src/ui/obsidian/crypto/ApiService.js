import { vcl } from './visualcrypt-light'
import { ResponsePayload, RequestObject } from './visualcrypt-dtos'
import { showError, showNotification, showWarning } from '../../renderer/reducers/notification'
export default class ApiService {
  static serverPublicKey
  static clientPublicKey
  /**
   * makeRequest - Return a ResponsePayload when an error ccured.
   *
   * @param  {RequestObject} requestObject - The parameter object for the API request.
   * @returns {ResponsePayload} ResponsePayload -
   */
  static makeRequest = requestObject => {
    if (!requestObject || !requestObject.command || !requestObject.ipAddress) {
      return Promise.resolve(ApiService.fail(0, 'Invalid arguments.', requestObject))
    }
    try {
      window.x1Store.dispatch({
        type: 'X1_FNC_SETISLOADING',
        command: requestObject.command,
        isLoading: true,
      })

      let request, expectedAuthKey

      if (requestObject.command === 'getKey') {
        var clientKeyPair = vcl.generateKeyPair()
        ApiService.clientPrivateKey = clientKeyPair.private
        ApiService.clientPublicKey = clientKeyPair.public
        request = vcl.createModel(ApiService.clientPublicKey)
      } else {
        if (!ApiService.serverPublicKey) {
          return Promise.resolve(ApiService.fail(0, 'No server public keys.', requestObject))
        } else {
          expectedAuthKey = vcl.hexStringToBytes(requestObject.expectedAuthKey)
          var json = JSON.stringify(requestObject)
          var jsonBytes = new TextEncoder().encode(json)
          var cipherV2Bytes = vcl.encrypt(
            jsonBytes,
            ApiService.serverPublicKey,
            expectedAuthKey,
            ApiService.clientPrivateKey
          )
          request = vcl.createModel(ApiService.clientPublicKey, cipherV2Bytes)
        }
      }
      return ApiService.post(this.getApiUrl(requestObject.ipAddress), request).then(
        response => {
          ApiService.serverPublicKey = vcl.hexStringToBytes(response.currentPublicKey)
          let responsePayload
          if (response.cipherV2Bytes) {
            var decrypted = vcl.decrypt(
              vcl.hexStringToBytes(response.cipherV2Bytes),
              ApiService.serverPublicKey,
              expectedAuthKey,
              ApiService.clientPrivateKey
            )
            var json = new TextDecoder().decode(decrypted)
            responsePayload = JSON.parse(json)
            responsePayload.authKey = response.authKey
            if (responsePayload.status !== 200) {
              if (responsePayload.status === 427) {
                ApiService.makeRequest(new RequestObject('getKey', ''))
              }
              return Promise.resolve(
                ApiService.fail(responsePayload.status, responsePayload.statusText, requestObject)
              )
            }
          } else {
            responsePayload = new ResponsePayload()
            responsePayload.responsePayload = response
            responsePayload.status = 200
            responsePayload.statusText = 'Ok'
            responsePayload.authKey = response.authKey
          }
          ApiService.end()
          return Promise.resolve(responsePayload)
        },
        error => {
          const status = error.status ? error.status : 0
          const statusText = !error.status ? error.message : error.statusText
          return Promise.resolve(ApiService.fail(status, statusText, requestObject))
        }
      )
    } catch (e) {
      return Promise.resolve(ApiService.fail(0, e.message, requestObject))
    }
  }

  static post = (url = '', data = {}) => {
    // Default options are marked with *
    return fetch(url, {
      method: 'POST', // *GET, POST, PUT, DELETE, etc.
      mode: 'cors', // no-cors, cors, *same-origin
      cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
      credentials: 'omit', // include, *same-origin, omit
      headers: {
        'Content-Type': 'application/json',
      },
      redirect: 'follow', // manual, *follow, error
      referrer: 'no-referrer', // no-referrer, *client
      body: JSON.stringify(data), // body data type must match "Content-Type" header
    }).then(response => {
      return response.json()
    }) // parses JSON response into native JavaScript objects
  }

  static getApiUrl = ipAddress => {
    return 'http://' + ipAddress + ':' + '37777' + '/SecureApi/ExecuteAsync'
  }

  /**
   * fail - Return a ResponsePayload when an error ccured.
   *
   * @param  {number} status - A valid HTTP status code, or 0, if an error occured before reaching the server, e.g. connection refused.
   * @param  {string} statusText - A text describing the error.
   * @returns {ResponsePayload} ResponsePayload
   */
  static fail = (status, statusText, requestObject) => {
    const responsePayload = new ResponsePayload()
    responsePayload.status = status
    responsePayload.statusText = `${statusText}`
    const errorText = `Request '${requestObject.command}' failed: ${responsePayload.status} - ${responsePayload.statusText}`
    console.log(errorText)
    if (status !== 0) {
      // is status is 0, it's normally 'No server public keys.', which is normal and should not be shown the the user as error
      window.x1Store.dispatch(showWarning(errorText))
      window.x1Store.dispatch({ type: 'X1_FNC_SETERRORTEXT', errorText })
    }

    ApiService.end()
    return responsePayload
  }

  static end = () => {
    window.x1Store.dispatch({ type: 'X1_FNC_SETISLOADING', command: '', isLoading: false })
  }
}

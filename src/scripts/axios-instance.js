import axios from 'axios';
import Cookies from 'js-cookie';

var url = 'https://kinobotz.herokuapp.com';
if(process.env.NODE_ENV === 'development') {
    url = 'https://localhost:44305';
}

const instance = axios.create({
  baseURL: url,
  headers: {
    'Content-Type': 'application/json',
  },
});

instance.interceptors.request.use(config => {
  var jwtToken = Cookies.get('jwtToken')
  if (jwtToken) {
    config.headers.Authorization = `Bearer ${jwtToken}`;
  }
  return config;
});

export default instance;
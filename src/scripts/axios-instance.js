import axios from 'axios';
import Cookies from 'js-cookie';

const url = process.env.VUE_APP_API_URL || 'https://kinobotz.herokuapp.com';

const instance = axios.create({
  baseURL: url,
  headers: {
    'Content-Type': 'application/json',
  },
});

instance.interceptors.request.use(config => {
  const jwtToken = Cookies.get('jwtToken');
  if (jwtToken) {
    config.headers.Authorization = `Bearer ${jwtToken}`;
  }
  return config;
});

export default instance;
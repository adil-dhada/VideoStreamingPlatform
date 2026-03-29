import React, { useState } from 'react';
import { useDispatch } from 'react-redux';
import { useNavigate } from 'react-router-dom';
import api from '../api/axios';
import { loginEvent } from '../store/authSlice';

const Login: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const dispatch = useDispatch();
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const response = await api.post('/auth/login', { email, password });
      dispatch(loginEvent(response.data.token));
      navigate('/');
    } catch (err) {
      alert('Login failed');
    }
  };

  return (
    <div style={{ padding: '2rem' }}>
      <h2>Login</h2>
      <form onSubmit={handleLogin}>
        <div><input type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="Email" required /></div>
        <br />
        <div><input type="password" value={password} onChange={e => setPassword(e.target.value)} placeholder="Password" required /></div>
        <br />
        <button type="submit">Log in</button>
      </form>
    </div>
  );
};

export default Login;

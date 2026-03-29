import React from 'react';
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import Login from './pages/Login';
import VideoPlayer from './pages/VideoPlayer';
import Upload from './pages/Upload';

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <nav style={{ padding: '1rem', background: '#222', color: '#fff', marginBottom: '2rem' }}>
        <Link to="/login" style={{ color: 'white', marginRight: '1rem', textDecoration: 'none' }}>Login</Link>
        <Link to="/upload" style={{ color: 'white', marginRight: '1rem', textDecoration: 'none' }}>Upload</Link>
        <Link to="/player/sample-guid" style={{ color: 'white', textDecoration: 'none' }}>Demo Player</Link>
      </nav>
      <div style={{ padding: '0 2rem' }}>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/upload" element={<Upload />} />
          <Route path="/player/:videoId" element={<VideoPlayer />} />
          <Route path="/" element={<div style={{ padding: '2rem' }}><h1>Home</h1><p>Welcome to Video Streaming Platform</p></div>} />
        </Routes>
      </div>
    </BrowserRouter>
  );
};

export default App;

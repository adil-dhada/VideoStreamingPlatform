import React, { useState } from 'react';
import api from '../api/axios';

const Upload: React.FC = () => {
  const [file, setFile] = useState<File | null>(null);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [status, setStatus] = useState('');
  const [progress, setProgress] = useState(0);

  const handleUpload = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) return;

    setStatus('Initiating...');
    try {
      const initRes = await api.post('/upload/initiate', {
        fileName: file.name,
        fileSizeBytes: file.size,
        totalChunks: Math.ceil(file.size / (5 * 1024 * 1024)),
        title,
        description
      });
      const { sessionId, chunkSizeBytes } = initRes.data;

      const totalChunks = Math.ceil(file.size / chunkSizeBytes);
      for (let i = 0; i < totalChunks; i++) {
        const start = i * chunkSizeBytes;
        const end = Math.min(start + chunkSizeBytes, file.size);
        const chunk = file.slice(start, end);
        
        await api.put(`/upload/${sessionId}/chunk/${i}`, chunk, {
          headers: { 'Content-Type': 'application/octet-stream' }
        });
        setProgress(Math.round(((i + 1) / totalChunks) * 100));
      }

      setStatus('Finalizing...');
      await api.post(`/upload/${sessionId}/finalize`, { title, description });
      setStatus('Upload Complete! Transcoding started in background.');

    } catch (err) {
      console.error(err);
      setStatus('Upload failed.');
    }
  };

  return (
    <div style={{ padding: '2rem' }}>
      <h2>Upload Video</h2>
      <form onSubmit={handleUpload}>
        <div style={{ marginBottom: '1rem' }}>
           <input type="text" placeholder="Title" value={title} onChange={e => setTitle(e.target.value)} required />
        </div>
        <div style={{ marginBottom: '1rem' }}>
           <textarea placeholder="Description" value={description} onChange={e => setDescription(e.target.value)} />
        </div>
        <div style={{ marginBottom: '1rem' }}>
           <input type="file" onChange={e => setFile(e.target.files?.[0] || null)} required />
        </div>
        <button type="submit" disabled={!file}>Upload</button>
      </form>
      {progress > 0 && progress < 100 && <p>Progress: {progress}%</p>}
      <p>{status}</p>
    </div>
  );
};

export default Upload;

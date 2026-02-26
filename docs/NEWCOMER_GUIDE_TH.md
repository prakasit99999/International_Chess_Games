# คู่มือสำหรับผู้เริ่มต้น: International_Chess_Games

## ภาพรวมโครงสร้างปัจจุบัน
ปัจจุบันรีโปนี้ยังเป็นโครงสร้างเริ่มต้น (bootstrap) และมีไฟล์หลักเพียง:

- `README.md` — ระบุชื่อโปรเจกต์
- `docs/NEWCOMER_GUIDE_TH.md` — คู่มือฉบับนี้สำหรับ onboarding

กล่าวคือ ตอนนี้ยังไม่มีโค้ดแอปพลิเคชัน, โมดูล backend/frontend, test suite, หรือไฟล์คอนฟิก build/deploy อย่างเป็นรูปธรรม

## สิ่งสำคัญที่ผู้มาใหม่ควรรู้
1. **สถานะโปรเจกต์อยู่ในช่วงตั้งต้น**  
   สิ่งที่เห็นสะท้อนว่า repository ยังไม่เข้าสู่ระยะ implementation

2. **ยังไม่มี source-of-truth ด้านสถาปัตยกรรม**  
   ยังไม่มีเอกสาร architecture (เช่น ADR, system diagram) และยังไม่เห็นการเลือกเทคโนโลยีหลัก

3. **ยังไม่เห็นมาตรฐานการพัฒนาในรีโป**  
   เช่น naming convention, branching policy, lint/test standard, CI pipeline

4. **โอกาสในการวางรากฐานสูง**  
   ช่วงนี้เหมาะกับการกำหนดโครงสร้างโฟลเดอร์, coding standard, testing strategy และเอกสารทีม

## แผนการเรียนรู้/ศึกษาต่อ (ลำดับแนะนำ)
1. **กำหนดขอบเขตโปรเจกต์ให้ชัด (Domain + Use Cases)**
   - โปรเจกต์ต้องการเก็บเกมหมากรุกในรูปแบบใด (PGN, metadata, player profile)
   - ผู้ใช้หลักคือใคร (นักวิเคราะห์เกม, ผู้เล่นทั่วไป, ผู้จัดการแข่งขัน)

2. **เลือก stack และวาง architecture เบื้องต้น**
   - Backend (เช่น Node/Python/Go)
   - Database (เช่น PostgreSQL + index สำหรับ query เกม)
   - Frontend (ถ้ามี)
   - โครงสร้างโมดูล: `src/`, `tests/`, `docs/`, `scripts/`

3. **ออกแบบ data model รุ่นแรก**
   - Entities สำคัญ: `players`, `games`, `events`, `openings`, `moves`
   - นิยาม primary key / foreign key และ query หลักที่ต้องรองรับ

4. **ตั้ง baseline ด้านคุณภาพโค้ด**
   - Lint + formatter
   - Unit/Integration test
   - CI ที่รันอัตโนมัติทุก PR

5. **ทำเอกสาร onboarding เพิ่มเติม**
   - วิธีรันโปรเจกต์ local
   - วิธีรันทดสอบ
   - แนวทางตั้งชื่อ branch/commit/PR

## Checklist ที่ควรมีใน milestone ถัดไป
- [ ] README ที่อธิบายวัตถุประสงค์, stack, และวิธีรัน
- [ ] โครงสร้างโฟลเดอร์มาตรฐาน
- [ ] ตัวอย่างข้อมูลเกมหมากรุก 1 ชุด
- [ ] test แรกที่รันผ่านใน CI
- [ ] เอกสารสถาปัตยกรรมฉบับย่อ

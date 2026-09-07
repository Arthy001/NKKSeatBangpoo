# 📄 เอกสารข้อกำหนดและมาตรฐานการ Export ไฟล์ (Export Specification)

## 1. บทนำและวัตถุประสงค์ (Overview & Objective)
เอกสารฉบับนี้กำหนดมาตรฐานรูปแบบไฟล์ (File Format) โครงสร้างข้อมูล (Data Layout) และหลักการตั้งชื่อไฟล์ (Naming Convention) สำหรับระบบ **Gate Entry Export (การส่งออกข้อมูลการสแกนผ่านประตูคลัง/จ่ายสินค้า)** ของระบบ NKK Seat Bangpoo เพื่อส่งต่อให้ระบบ ERP/ระบบบริหารจัดการต่อไป

---

## 2. หลักการตั้งชื่อไฟล์ (File Naming Convention)

รูปแบบมาตรฐานของชื่อไฟล์ Export:
`122_workorder_{Manufacturer}_{ddMMyyyyHHmmss}.csv`

### รายละเอียดตัวแปร:
- **122** : รหัสสาขา / รหัสโรงงาน (Site Code)
- **workorder** : ประเภทรายการข้อมูล (Work Order Dispatch Transaction)
- **{Manufacturer}** : ชื่อผู้ผลิต/ลูกค้าหลัก (ตรวจจับจาก Work Center ของชิ้นงาน)
- **{ddMMyyyyHHmmss}** : วันที่และเวลาส่งออก (วันเดือนปี ชั่วโมงนาทีวินาที)
  - dd = วัน (01-31)
  - MM = เดือน (01-12)
  - yyyy = ปี ค.ศ. (เช่น 2026)
  - HH = ชั่วโมง (00-23)
  - mm = นาที (00-59)
  - ss = วินาที (00-59)

### ตารางตัวอย่างชื่อไฟล์:

| ผู้ผลิต / ลูกค้า | กฎการตั้งชื่อ (Naming Rule) | ตัวอย่างชื่อไฟล์ |
| :--- | :--- | :--- |
| 🚙 **TOYOTA** | `122_workorder_Toyata_ddMMyyyyHHmmss.csv` | `122_workorder_Toyata_07092026092030.csv` |
| 🚗 **ISUZU** | `122_workorder_Isuzu_ddMMyyyyHHmmss.csv` | `122_workorder_Isuzu_07092026092030.csv` |
| 🛻 **NISSAN** | `122_workorder_Nissan_ddMMyyyyHHmmss.csv` | `122_workorder_Nissan_07092026092030.csv` |
| 📦 **อื่นๆ (Other)** | `122_workorder_{Manufacturer}_ddMMyyyyHHmmss.csv` | `122_workorder_Other_07092026092030.csv` |

---

## 3. โครงสร้างข้อมูลภายในไฟล์ (Data Structure & Format)

- **Encoding:** UTF-8 with BOM (`\uFEFF`) เพื่อให้อ่านภาษาไทยและเปิดบน Microsoft Excel ได้อย่างถูกต้อง

---

### 3.1 รูปแบบไฟล์ TOYOTA (Format ใหม่)
- **Header:**
  `Site,Work Order,Part no.,Pallet,Skid No.,PW No.ตัวแม่,Part no. ลูก`
- **หลักการคำนวณ Pallet และ Skid No.:**
  - **Pallet:** เลข Pallet ถูกผูกกับ **Work Order** (1 Work Order = 1 Pallet ซึ่งประกอบด้วย 4 เบาะ)
    - Work Order ลำดับที่ 1 ➔ `Pallet = 1`
    - Work Order ลำดับที่ 2 ➔ `Pallet = 2`
    - Work Order ลำดับที่ 3 ➔ `Pallet = 3`
    - Work Order ลำดับที่ 4 ➔ `Pallet = 4` (วนรอบ 1..4)
  - **Skid No.:** มีค่าคงที่เท่ากับ **`8`** ในทุกบรรทัด
- **รายละเอียดคอลัมน์:**
  1. `Site` : รหัสสาขา (ค่าคงที่ `122`)
  2. `Work Order` : เลขที่คำสั่งผลิต (Order No เช่น `20260311037S`)
  3. `Part no.` : Customer Part No (เช่น `71001-F0J61-C0`)
  4. `Pallet` : ลำดับ Pallet ตามรอบของ Work Order (1 ถึง 4)
  5. `Skid No.` : หมายเลข Skid (ค่าคงที่ `8`)
  6. `PW No.ตัวแม่` : `KanbanSet` (ดึงจาก Column O ในไฟล์ Master เช่น `040922H445`)
  7. `Part no. ลูก` : `KanbanCode` (ดึงจาก Column K ในไฟล์ Master เช่น `041789H445`)
- **ตัวอย่างข้อมูล:**
  ```text
  Site,Work Order,Part no.,Pallet,Skid No.,PW No.ตัวแม่,Part no. ลูก
  122,20260311037S,71001-F0J61-C0,1,8,040922H445,041789H445
  122,20260311037S,71001-F0J61-C1,1,8,040922H445,041788H445
  122,20260311037S,71003-0K280-D4,1,8,024123E432,024125E432
  122,20260311037S,71003-0K280-D4,1,8,024123E432,024124E432
  122,20260311038S,71003-0K370-C7,2,8,022080E433,022082E433
  122,20260311038S,71003-0K370-C8,2,8,022080E433,022081E433
  122,20260311038S,71001-F0K31-D0,2,8,040936H450,041828H450
  122,20260311038S,71001-F0K31-D0,2,8,040936H450,041828H450
  ```

---

### 3.2 รูปแบบไฟล์ ISUZU (Format ใหม่)
- **Header:**
  `Work Order,Line,Slip,Item Number,Qty Pending`
- **หลักการคำนวณเลข Slip (Auto Slip Calculation):**
  - **ไม่ดึงจาก Database** แต่ระบบจะทำการคำนวณอัตโนมัติขณะ Export
  - **กฎ:** 1 Slip บรรจุได้สูงสุด 10 รายการ (Lines) ต่อ 1 Work Order
  - **สูตรคำนวณ:**
    `Slip = ((Line - 1) / 10) + 1`
    - Line 1 – 10 ➔ **Slip 1**
    - Line 11 – 20 ➔ **Slip 2**
    - Line 21 – 30 ➔ **Slip 3**
    - รีเซ็ตเป็น 1 ใหม่เมื่อขึ้น Work Order ใหม่
- **รายละเอียดคอลัมน์:**
  1. `Work Order` : เลขที่คำสั่งผลิต (เช่น `S0009140`)
  2. `Line` : สายการผลิต / ลำดับ Line (เช่น `1`, `2`, `3`)
  3. `Slip` : หมายเลข Slip จากการคำนวณสูตร 10 รายการต่อ Slip
  4. `Item Number` : รหัสพาร์ท (NHK Part No / Item Number เช่น `NI2171-20000`)
  5. `Qty Pending` : จำนวนชิ้นงานรวมที่สแกนออก (Sum Qty เช่น `15`)
- **ตัวอย่างข้อมูล:**
  ```text
  Work Order,Line,Slip,Item Number,Qty Pending
  S0009140,1,1,NI2171-20000,15
  S0009140,2,1,NI3151-20000,15
  S0009141,1,1,NI2175-10000,15
  ...
  S0009141,10,1,NI2171-20007,15
  S0009141,11,2,NI2171-20008,15
  S0009141,12,2,NI2171-20009,15
  ```

---

### 3.3 รูปแบบไฟล์ NISSAN / อื่นๆ (Generic)
- **Header:**
  `EPC,Order No,Part No,Part Name,Line,Kanban,Read Time,Status,WorkCenter`
- **รายละเอียดคอลัมน์:**
  1. `EPC` : รหัส RFID Tag EPC (24 หลัก)
  2. `Order No` : เลขที่คำสั่งผลิต
  3. `Part No` : รหัสชิ้นงาน
  4. `Part Name` : ชื่อชิ้นงาน
  5. `Line` : หมายเลขไลน์
  6. `Kanban` : หมายเลขคัมบัง
  7. `Read Time` : เวลาที่สแกนผ่านประตู (yyyy-MM-dd HH:mm:ss)
  8. `Status` : สถานะ
  9. `WorkCenter` : ศูนย์งาน/ลูกค้า

---

## 4. โฟลเดอร์ปลายทางและการบันทึกข้อมูล (Destination & Database Update)

1. **Path Configuration:**
   - ผู้ใช้สามารถเลือกโฟลเดอร์ Local Drive หรือ Network Shared Path (`\\192.168.x.x\SharedExport`) ได้ที่หน้าจอ Gate Entry
   - มีระบบจำลอง Network Credential (Domain/Username/Password) อัตโนมัติในตัว
2. **Database Traceability Update:**
   - เมื่อ Export สำเร็จ ระบบจะอัปเดตตาราง `TagBindingTransaction` อัตโนมัติ:
     - `IsExported = 1`
     - `ExportFileName = '{ชื่อไฟล์ที่ Export}'`

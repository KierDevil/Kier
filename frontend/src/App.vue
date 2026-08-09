<template>
  <div class="workspace">
    <aside class="sidebar">
      <div class="brand">
        <span class="brand-mark">K</span>
        <div>
          <strong>Kier Records</strong>
          <small>Financial records system</small>
        </div>
      </div>

      <nav class="nav-list" aria-label="Main sections">
        <button
          v-for="item in navItems"
          :key="item.id"
          type="button"
          :class="{ active: activeView === item.id }"
          @click="setView(item.id)"
        >
          <span>{{ item.short }}</span>
          {{ item.label }}
        </button>
      </nav>

      <section class="sidebar-summary" aria-label="Current balance">
        <span>Available Funds</span>
        <strong>{{ money(availableFunds) }}</strong>
        <small>{{ collections.length }} receipts, {{ disbursements.length }} expenses</small>
      </section>

      <div class="api-card">
        <span class="status-dot" :class="{ online: apiOnline }"></span>
        <div>
          <strong>{{ apiOnline ? 'Backend online' : 'Frontend mode' }}</strong>
          <small>{{ healthDetail }}</small>
        </div>
      </div>
    </aside>

    <main class="content">
      <header class="topbar">
        <div>
          <p class="eyebrow">{{ activeSection.eyebrow }}</p>
          <h1>{{ activeSection.title }}</h1>
        </div>

        <div class="topbar-actions">
          <span v-if="isLoggedIn" class="user-badge">{{ authUser.username }} · {{ authUser.role }}</span>
          <button type="button" class="login-toggle" @click="isLoggedIn ? logout() : openLogin()">
            {{ isLoggedIn ? 'Logout' : 'Login' }}
          </button>
        </div>
      </header>

      <p v-if="toastMessage" class="toast">{{ toastMessage }}</p>

      <div v-if="loginOpen" class="auth-overlay" @click.self="loginOpen = false">
        <form class="auth-card" @submit.prevent="login">
          <div class="auth-header">
            <div>
              <span class="auth-label">Secure access</span>
              <h2>Department sign in</h2>
              <p>Enter your username and password to continue.</p>
            </div>
            <button type="button" class="text-action" @click="loginOpen = false">Close</button>
          </div>

          <div class="auth-field">
            <label for="login-username">Username</label>
            <input id="login-username" v-model="loginForm.username" type="text" required placeholder="Your username" />
          </div>

          <div class="auth-field">
            <label for="login-password">Password</label>
            <input id="login-password" v-model="loginForm.password" type="password" required placeholder="Your password" />
          </div>

          <button type="submit" class="primary-action">Sign in</button>
          <small class="auth-hint">Default account: admin / Admin123!</small>
        </form>
      </div>

      <section v-if="activeView === 'dashboard'" class="view-stack">
        <div class="stat-grid">
          <article v-for="stat in stats" :key="stat.label" class="stat-card">
            <span>{{ stat.label }}</span>
            <strong>{{ stat.value }}</strong>
            <small>{{ stat.detail }}</small>
          </article>
        </div>

        <div class="split-grid">
          <section class="panel">
            <div class="panel-heading">
              <div>
                <h2>Monthly Cash Flow</h2>
                <span>Collections against disbursements</span>
              </div>
            </div>
            <div class="chart-list">
              <div v-for="item in cashFlow" :key="item.month" class="chart-row">
                <span>{{ item.month }}</span>
                <div class="bar-track">
                  <span class="bar income" :style="{ width: `${item.incomeWidth}%` }"></span>
                  <span class="bar expense" :style="{ width: `${item.expenseWidth}%` }"></span>
                </div>
                <strong>{{ money(item.net) }}</strong>
              </div>
            </div>
          </section>

          <section class="panel">
            <div class="panel-heading">
              <div>
                <h2>Recent Activity</h2>
                <span>Latest changes in this workspace</span>
              </div>
            </div>
            <ul class="activity-list">
              <li v-for="item in activity.slice(0, 7)" :key="item.id">
                <span>{{ item.type }}</span>
                <div>
                  <strong>{{ item.title }}</strong>
                  <small>{{ item.detail }}</small>
                </div>
              </li>
            </ul>
          </section>
        </div>

        <section class="panel">
          <div class="panel-heading">
            <div>
              <h2>Priority Students</h2>
              <span>Students with balances or recent attendance concerns</span>
            </div>
            <button type="button" @click="setView('students')">Open Directory</button>
          </div>
          <table>
            <thead>
              <tr>
                <th>Student</th>
                <th>Course</th>
                <th>Balance</th>
                <th>Attendance</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="student in priorityStudents" :key="student.id" @click="selectStudent(student.id)">
                <td>{{ student.studentNo }} - {{ student.name }}</td>
                <td>{{ student.course }} {{ student.yearLevel }}</td>
                <td>{{ money(balanceFor(student.id)) }}</td>
                <td>{{ attendanceFor(student.id) }}%</td>
              </tr>
            </tbody>
          </table>
        </section>
        
      </section>

      <section v-else-if="activeView === 'students'" class="data-layout">
        <form class="panel form-panel" @submit.prevent="saveStudent">
          <div class="form-title">
            <h2>{{ editingStudentId ? 'Edit Student' : 'Add Student' }}</h2>
            <button v-if="editingStudentId" type="button" class="text-action" @click="resetStudentForm">Cancel</button>
          </div>
          <label>
            Student ID
            <input v-model="studentForm.studentNo" type="text" required />
          </label>
          <label>
            First Name
            <input v-model="studentForm.firstName" type="text" required />
          </label>
          <label>
            Last Name
            <input v-model="studentForm.lastName" type="text" required />
          </label>
          <label>
            Suffix
            <input v-model="studentForm.suffix" type="text" placeholder="Jr., Sr., III" />
          </label>
          <label>
            Course
            <input v-model="studentForm.course" type="text" required />
          </label>
          <label>
            Year
            <input v-model="studentForm.yearLevel" type="text" required />
          </label>
          <label>
            Contact
            <input v-model="studentForm.contact" type="text" />
          </label>
          <label>
            Email
            <input v-model="studentForm.email" type="email" placeholder="name@example.com" />
          </label>
          <label>
            RFID UID
            <input v-model="studentForm.rfidUid" type="text" placeholder="Tap card or type UID" />
          </label>
          <button type="submit" class="primary-action">{{ editingStudentId ? 'Update Student' : 'Save Student' }}</button>
        </form>

        <section class="panel">
          <div class="panel-heading">
            <div>
              <h2>Student Directory</h2>
              <span>{{ filteredStudents.length }} records</span>
            </div>
            <div class="table-controls">
              <label>
                Sort by
                <select v-model="studentSortField">
                  <option value="name">Name</option>
                  <option value="studentNo">ID</option>
                  <option value="course">Course</option>
                  <option value="balance">Balance</option>
                </select>
              </label>
              <label>
                Order
                <select v-model="studentSortDirection">
                  <option value="asc">Ascending</option>
                  <option value="desc">Descending</option>
                </select>
              </label>
              <label>
                Group by
                <select v-model="studentGroupBy">
                  <option value="none">None</option>
                  <option value="course">Course</option>
                </select>
              </label>
            </div>
          </div>
          <table> 
            <thead>
              <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Course</th>
                <th>RFID</th>
                <th>Balance</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              <template v-if="studentGroupBy === 'none'">
                <tr v-for="student in sortedStudents" :key="student.id">
                  <td>{{ student.studentNo }}</td>
                  <td>
                    <button type="button" class="row-link" @click="selectStudent(student.id)">{{ student.name }}</button>
                  </td>
                  <td>{{ student.course }} {{ student.yearLevel }}</td>
                  <td>{{ student.rfidUid || 'Not mapped' }}</td>
                  <td>{{ money(balanceFor(student.id)) }}</td>
                  <td class="table-actions">
                    <button type="button" @click="editStudent(student)">Edit</button>
                    <button type="button" @click="removeStudent(student.id)">Delete</button>
                  </td>
                </tr>
              </template>
              <template v-else>
                <template v-for="group in groupedStudents" :key="group.group">
                  <tr class="group-row">
                    <td colspan="6"><strong>{{ group.group }}</strong></td>
                  </tr>
                  <tr v-for="student in group.items" :key="student.id">
                    <td>{{ student.studentNo }}</td>
                    <td>
                      <button type="button" class="row-link" @click="selectStudent(student.id)">{{ student.name }}</button>
                    </td>
                    <td>{{ student.course }}</td>
                    <td>{{ student.rfidUid || 'Not mapped' }}</td>
                    <td>{{ money(balanceFor(student.id)) }}</td>
                    <td class="table-actions">
                      <button type="button" @click="editStudent(student)">Edit</button>
                      <button type="button" @click="removeStudent(student.id)">Delete</button>
                    </td>
                  </tr>
                </template>
              </template>
            </tbody>
          </table>
        </section>

        

      </section>

      <section v-else-if="activeView === 'collections'" class="collections-page">
        <div class="collections-forms">
          <form class="panel form-panel" @submit.prevent="addCollection">
            <h2>Add New Collection</h2>
            <label>
              Student
              <div class="autocomplete">
                <input v-model="collectionStudentName" type="text" placeholder="Type student name" required @input="handleCollectionStudentNameInput" />
                <ul v-if="collectionNameSuggestions.length" class="autocomplete-list">
                  <li v-for="suggestion in collectionNameSuggestions" :key="suggestion" @mousedown.prevent="selectCollectionStudentName(suggestion)">
                    {{ suggestion }}
                  </li>
                </ul>
              </div>
            </label>
            <label>
              Category
              <input list="collection-categories" v-model="collectionForm.category" type="text" placeholder="Choose or type category" required />
              <datalist id="collection-categories">
                <option v-for="category in collectionCategories" :key="category" :value="category"></option>
              </datalist>
            </label>
            <label>
              Status
              <select v-model="collectionForm.status">
                <option>Paid</option>
                <option>Unpaid</option>
              </select>
            </label>
            <label>
              Amount
              <input v-model.number="collectionForm.amount" type="number" min="1" step="1" />
            </label>
            <label>
              Receipt
              <input :value="collectionForm.receipt" type="text" readonly placeholder="Auto-generated receipt" />
            </label>
            <label>
              Send receipt to
              <input v-model="receiptEmail" type="email" placeholder="Enter recipient email to send receipt" />
              <small class="field-note">Email receipt is only sent if you enter an email address.</small>
            </label>
            <button type="submit" class="primary-action">{{ editingCollectionId ? 'Update Payment' : 'Save Payment' }}</button>
          </form>

        </div>

        <section class="panel ledger-panel">
          <div class="panel-heading compact-heading">
            <div>
              <h2>Ledger</h2>
              <span>{{ filteredCollections.length }} visible bill or receipt entries</span>
            </div>
            <div class="ledger-total">
              <span>Total</span>
              <strong>{{ money(totalCollections) }}</strong>
            </div>
          </div>

          <div class="ledger-list">
            <article v-for="collection in filteredCollections" :key="collection.id" class="ledger-item">
              <div class="ledger-main">
                <div class="ledger-meta">
                  <strong>{{ collection.receipt }}</strong>
                  <span>{{ studentName(collection.studentId) }}</span>
                </div>
                <div class="ledger-category">
                {{ collection.category }}
                <span class="badge" :class="{ paid: collection.status === 'Paid' }">{{ collection.status }}</span>
              </div>
              </div>
              <div class="ledger-actions">
                <div class="ledger-amount">{{ money(collection.amount) }}</div>
                <button type="button" @click="editCollection(collection)">Edit</button>
                <button type="button" @click="toggleCollectionStatus(collection.id)">
                  {{ collection.status === 'Paid' ? 'Mark Unpaid' : 'Mark Paid' }}
                </button>
                <button type="button" @click="removeCollection(collection.id)">Delete</button>
              </div>
            </article>
          </div>
        </section>
      </section>

      <section v-else-if="activeView === 'admin'" class="view-stack">
        <div class="stat-grid">
          <article class="stat-card">
            <span>Total Owed</span>
            <strong>{{ money(totalOwed) }}</strong>
            <small>Unpaid bills plus fines</small>
          </article>
            <article class="stat-card">
              <span>Outstanding Bills</span>
              <strong>{{ money(outstandingBills) }}</strong>
              <small>{{ collections.filter((collection) => collection.status !== 'Paid').length }} open bills</small>
            </article>
            <article class="stat-card">
              <span>Unpaid Fines</span>
              <strong>{{ money(unpaidFines) }}</strong>
              <small>{{ fines.filter((fine) => fine.status !== 'Paid').length }} open fines</small>
            </article>
        </div>

        <section class="panel">
          <div class="panel-heading">
            <div>
              <h2>Student Dues</h2>
              <span>Review unpaid bills and fines by student</span>
            </div>
          </div>

          <table>
            <thead>
              <tr>
                <th>Student</th>
                <th>Course</th>
                <th>Unpaid Bills</th>
                <th>Unpaid Fines</th>
                <th>Total Due</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="student in studentOwed" :key="student.id">
                <td>{{ student.name }}</td>
                <td>{{ student.course }}</td>
                <td>{{ money(student.unpaidBills) }}</td>
                <td>{{ money(student.unpaidFineAmount) }}</td>
                <td>{{ money(student.totalDue) }}</td>
                <td class="table-actions">
                  <button type="button" @click="openAdminAddReceipt(student.id)">Add Receipt</button>
                  <button type="button" @click="openAdminAddFine(student.id)">Add Fine</button>
                </td>
              </tr>
            </tbody>
          </table>
        </section>
          <!-- Admin quick-actions: add bills, contributions, fines, funds -->
          <section class="panel admin-quick-actions">
            <div class="panel-heading">
              <div>
                <h2>Admin Quick Actions</h2>
                <span>Create or edit common financial items quickly</span>
              </div>
            </div>

            <div class="admin-grid">
              <form class="admin-card" @submit.prevent="addCollectionForAllStudents">
                <h3>Add Bill / Event Contribution</h3>
                <p>Creates one entry for every student.</p>
                <label>
                  Category
                  <input list="collection-categories" v-model="collectionForm.category" type="text" placeholder="e.g. Event Contribution, Department Fee" />
                </label>
                <label>
                  Amount
                  <input v-model.number="collectionForm.amount" type="number" min="0" step="1" />
                </label>
                <div class="button-row">
                  <button type="submit" class="primary-action">Create for All</button>
                </div>
              </form>

              <form class="admin-card" @submit.prevent="addDisbursement">
                <h3>Add Department Fund / Expense</h3>
                <label>
                  Description
                  <input v-model="disbursementForm.description" type="text" />
                </label>
                <label>
                  Used / Withdrawn by
                  <input v-model="disbursementForm.usedBy" type="text" placeholder="Officer or person responsible" />
                </label>
                <label>
                  Amount
                  <input v-model.number="disbursementForm.amount" type="number" min="0" step="1" />
                </label>
                <div class="button-row">
                  <button type="submit" class="primary-action">{{ editingDisbursementId ? 'Update' : 'Create' }}</button>
                </div>
              </form>

              <section class="admin-card reset-card">
                <h3>Reset all app data</h3>
                <p>Clear all students, collections, fines, attendance, emails, and activity, then start fresh.</p>
                <div class="button-row">
                  <button type="button" class="secondary-action" @click="confirmResetState">Reset Everything</button>
                </div>
              </section>
            </div>
          </section>
      </section>

      <section v-else-if="activeView === 'fines'" class="data-layout">
        <form class="panel form-panel" @submit.prevent="addFine">
          <h2>{{ editingFineId ? 'Edit Fine' : 'Add Fine' }}</h2>
          <label>
            Student
            <div class="autocomplete">
              <input v-model="fineStudentName" type="text" placeholder="Type student name" required @input="handleFineStudentNameInput" />
              <ul v-if="fineNameSuggestions.length" class="autocomplete-list">
                <li v-for="suggestion in fineNameSuggestions" :key="suggestion" @mousedown.prevent="selectFineStudentName(suggestion)">
                  {{ suggestion }}
                </li>
              </ul>
            </div>
          </label>
          <label>
            Category
            <input v-model="fineForm.category" type="text" required />
          </label>
          <label>
            Amount
            <input v-model.number="fineForm.amount" type="number" min="1" step="1" />
          </label>
          <label>
            Status
            <select v-model="fineForm.status">
              <option>Unpaid</option>
              <option>Paid</option>
            </select>
          </label>
          <button type="submit" class="primary-action">{{ editingFineId ? 'Update Fine' : 'Save Fine' }}</button>
        </form>

        <section class="panel">
          <div class="panel-heading">
            <div>
              <h2>Absent Fines by Student</h2>
              <span>Click a student name to view absent events and fines</span>
            </div>
          </div>
          <div class="absent-fines-browser">
            <div class="student-name-list">
              <button
                v-for="student in filteredStudents"
                :key="student.id"
                type="button"
                :class="{ active: selectedAbsentFineStudentId === student.id }"
                @click="selectedAbsentFineStudentId = student.id"
              >
                <span>{{ student.name }}</span>
                <strong>{{ absentFineCountFor(student.id) }}</strong>
              </button>
              <p v-if="!filteredStudents.length">No students found</p>
            </div>
            <div class="absent-fines-detail">
              <template v-if="selectedAbsentFineStudent">
                <div class="absent-detail-heading">
                  <div>
                    <h3>{{ selectedAbsentFineStudent.name }}</h3>
                    <span>{{ selectedStudentAbsentFines.length }} absent event(s)</span>
                  </div>
                  <div class="absent-total-actions">
                    <strong>{{ money(selectedStudentAbsentFineTotal) }}</strong>
                    <button
                      v-if="selectedStudentUnpaidAbsentFineTotal > 0"
                      type="button"
                      class="primary-action"
                      @click="payAllAbsentFinesForStudent(selectedAbsentFineStudent.id)"
                    >
                      Pay Total
                    </button>
                  </div>
                </div>
                <div v-if="selectedStudentAbsentFines.length" class="absent-event-list">
                  <article v-for="item in selectedStudentAbsentFines" :key="item.key">
                    <div>
                      <strong>{{ item.event }}</strong>
                      <span>{{ formatRecordTime(item.recordedAt) || 'No time recorded' }}</span>
                    </div>
                    <div>
                      <strong>{{ money(item.amount) }}</strong>
                      <span>{{ item.status }}</span>
                    </div>
                    <button
                      v-if="item.fineId && item.status !== 'Paid'"
                      type="button"
                      class="secondary-action"
                      @click="payAbsentFine(item.fineId)"
                    >
                      Pay Event
                    </button>
                  </article>
                </div>
                <p v-else class="empty-state">No absent events recorded for this student.</p>
              </template>
              <p v-else class="empty-state">Select a student to view absent fines.</p>
            </div>
          </div>
        </section>
      </section>

      <section v-else-if="activeView === 'attendance'" class="view-stack">
        <section class="panel event-creator-panel">
          <div class="panel-heading">
            <div>
              <h2>Event</h2>
              <span>Enter the event name to start attendance.</span>
            </div>
          </div>
          <div class="panel-body">
            <div class="event-input-group">
              <label>
                Event
                <input list="attendance-events" v-model="scanForm.eventTitle" type="text" placeholder="Enter event name" />
                <datalist id="attendance-events">
                  <option v-for="ev in attendanceEvents" :key="ev.id" :value="ev.title"></option>
                </datalist>
                <small>Type event name and click Start Event. Existing events are suggested for reuse.</small>
              </label>
            </div>
            <div class="event-details-grid">
              <label>
                Type
                <select v-model="scanForm.sessionType">
                  <option>Log In</option>
                  <option>Log Out</option>
                </select>
              </label>
              <label>
                Start time
                <input v-model="scanForm.openTime" type="time" />
              </label>
              <label>
                Close time
                <input v-model="scanForm.closeTime" type="time" />
              </label>
              <label>
                Absent fine
                <input v-model.number="scanForm.absentFine" type="number" min="0" step="1" />
              </label>
            </div>
            <div class="button-row">
              <button type="button" class="primary-action" @click="createAttendanceEvent(scanForm.eventTitle)">Start Event</button>
              <button type="button" class="secondary-action" @click="closeCurrentAttendanceEvent">Close Event</button>
            </div>
          </div>
        </section>
        <section v-if="currentAttendanceEventId" class="qr-scanner-grid">
          <section class="panel scanner-panel">
            <div class="panel-heading">
              <div>
                <h2>Fast Attendance</h2>
                <span>RFID, QR, or student ID number</span>
              </div>
              <button type="button" @click="scannerActive ? stopQrScanner() : startQrScanner()">
                {{ scannerActive ? 'Stop Camera' : 'Start Camera' }}
              </button>
            </div>
            <div class="scanner-body">
              <div class="scanner-controls">
                <div class="fast-scan-strip">
                  <label>
                    Fast Scan
                    <input
                      ref="quickScanInput"
                      v-model="quickScanValue"
                      type="text"
                      placeholder="Tap RFID, scan QR, or type student ID"
                      @keydown.enter.prevent="recordAnyScan(quickScanValue)"
                    />
                  </label>
                  <button type="button" class="primary-action" @click="recordAnyScan(quickScanValue)">Record</button>
                </div>

                <details class="scan-settings">
                  <summary>
                    <span>Settings</span>
                    <strong>{{ scanForm.eventTitle }} - {{ scanForm.status }}</strong>
                  </summary>
                  <div class="event-controls">
                    <div class="current-event-display">
                      <strong>Current Event:</strong>
                      <span>{{ scanForm.eventTitle || 'No event selected' }} - {{ scanForm.sessionType }} - {{ money(scanForm.absentFine || 0) }} absent fine</span>
                    </div>
                  </div>
                  <div class="compact-grid">
                    <label>
                      Status
                      <select v-model="scanForm.status">
                        <option>Present</option>
                        <option>Late</option>
                        <option>Absent</option>
                        <option>Excused</option>
                      </select>
                    </label>
                  </div>

                  <div class="compact-grid">
                    <label>
                      Fine / Min
                      <input v-model.number="scanForm.finePerLateMinute" type="number" min="0" step="1" />
                    </label>
                    <label>
                      Max Fine
                      <input v-model.number="scanForm.maxLateFine" type="number" min="0" step="1" />
                    </label>
                  </div>
                </details>

                <div class="scan-fallbacks">
                  <label>
                    ID / QR
                    <input
                      v-model="manualQr"
                      type="text"
                      placeholder="1162304531"
                      @keydown.enter.prevent="recordQrScan(manualQr)"
                    />
                  </label>
                  <button type="button" class="secondary-action" @click="recordQrScan(manualQr)">ID / QR</button>
                  <label>
                    RFID
                    <input
                      v-model="manualRfid"
                      type="text"
                      placeholder="Tap card"
                      @keyup.enter="recordRfidScan(manualRfid)"
                    />
                  </label>
                  <button type="button" class="secondary-action" @click="recordRfidScan(manualRfid)">RFID</button>
                  <label class="photo-input">
                    QR Photo
                    <input type="file" accept="image/*" capture="environment" @change="recordQrPhoto" />
                  </label>
                </div>
              </div>
              <div class="camera-slot" :class="{ idle: !scannerActive }">
                <video v-show="scannerActive" ref="scannerVideo" class="scanner-video" autoplay muted playsinline></video>
                <span v-if="!scannerActive">Camera off</span>
              </div>
              <p class="scanner-message">{{ scannerMessage }}</p>
              <transition name="scan-pop">
                <aside v-if="scanPop.visible" class="scan-pop-card" :class="scanPop.status.toLowerCase()">
                  <span>{{ scanPop.method }}</span>
                  <strong>{{ scanPop.name }}</strong>
                  <small>{{ scanPop.studentNo }} - {{ scanPop.status }} - {{ scanPop.event }}</small>
                  <em>{{ scanPop.time }}</em>
                </aside>
              </transition>
            </div>
          </section>

          <details v-if="currentAttendanceEventId" class="panel manual-panel">
            <summary>Manual Attendance</summary>
            <form class="manual-form" @submit.prevent="addAttendance">
              <label>
                Event
                <select v-model.number="currentAttendanceEventId">
                  <option :value="null">-- select event --</option>
                  <option v-for="ev in attendanceEvents" :key="ev.id" :value="ev.id">{{ ev.title }}</option>
                </select>
                <small v-if="!attendanceEvents.length">No events defined — create one in Settings.</small>
              </label>
              <label>
                Student
                <select v-model.number="attendanceForm.studentId">
                  <option v-for="student in students" :key="student.id" :value="student.id">{{ student.name }}</option>
                </select>
              </label>
              <label>
                Status
                <select v-model="attendanceForm.status">
                  <option>Present</option>
                  <option>Late</option>
                  <option>Absent</option>
                  <option>Excused</option>
                </select>
              </label>
              <button type="submit" class="primary-action">Save</button>
            </form>
          </details>
        </section>

        <section class="panel">
          <div class="panel-heading">
            <div>
              <h2>Attendance Records</h2>
              <span>{{ filteredAttendance.length }} visible entries</span>
            </div>
            <strong>{{ attendanceRate }}%</strong>
          </div>
          <table>
            <thead>
              <tr>
                <th>Event</th>
                <th>Student</th>
                <th>Status</th>
                <th>Time In</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="record in filteredAttendance" :key="record.id">
                <td>{{ record.event }}</td>
                <td>{{ studentName(record.studentId) }}</td>
                <td><span class="badge neutral">{{ record.status }}</span></td>
                <td>{{ formatRecordTime(record.recordedAt) }}</td>
                <td class="table-actions">
                  <button type="button" @click="removeAttendance(record.id)">Delete</button>
                </td>
              </tr>
            </tbody>
          </table>
        </section>
      </section>

      <section v-else-if="activeView === 'emails'" class="view-stack">
        <div class="stat-grid">
          <article class="stat-card">
            <span>Sent Emails</span>
            <strong>{{ sentEmails.length }}</strong>
            <small>Fine notices delivered</small>
          </article>
          <article class="stat-card">
            <span>Pending Emails</span>
            <strong>{{ pendingEmails.length }}</strong>
            <small>Waiting for internet or backend email</small>
          </article>
          <article class="stat-card">
            <span>Last Sent</span>
            <strong>{{ sentEmails[0] ? formatRecordTime(sentEmails[0].sentAt) : 'None' }}</strong>
            <small>Most recent successful notice</small>
          </article>
        </div>

        <section class="panel form-panel">
          <div class="panel-heading">
            <div>
              <h2>Compose Email</h2>
              <span>Send a general or specific payment message</span>
            </div>
          </div>
          <label>
            Send to
            <select v-model="emailComposer.mode">
              <option value="general">General - all students with email</option>
              <option value="specific">Specific student</option>
            </select>
          </label>
          <label v-if="emailComposer.mode === 'specific'">
            Student
            <select v-model.number="emailComposer.studentId">
              <option :value="null">-- select student --</option>
              <option v-for="student in studentsWithEmail" :key="student.id" :value="student.id">
                {{ student.name }} - {{ student.email }}
              </option>
            </select>
          </label>
          <label>
            Subject
            <input v-model="emailComposer.subject" type="text" placeholder="Payment reminder" />
          </label>
          <label>
            Message
            <textarea
              v-model="emailComposer.message"
              rows="4"
              placeholder="Example: Please settle your unpaid balance. If you want to pay, contact the officer or visit the cashier."
            ></textarea>
          </label>
          <div class="button-row">
            <button type="button" class="primary-action" @click="sendComposedEmails">Send Message</button>
          </div>
        </section>

        <section class="panel">
          <div class="panel-heading">
            <div>
              <h2>Pending Email Outbox</h2>
              <span>{{ pendingEmails.length }} email(s) waiting</span>
            </div>
            <button v-if="pendingEmails.length" type="button" class="primary-action" @click="processPendingEmails">Send Pending</button>
          </div>
          <table>
            <thead>
              <tr>
                <th>Student</th>
                <th>Email</th>
                <th>Subject / Event</th>
                <th>Total</th>
                <th>Queued</th>
                <th>Last Tried</th>
                <th>Attempts</th>
                <th>Last Error</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="email in pendingEmails" :key="email.id">
                <td>{{ email.payload.studentName }}</td>
                <td>{{ email.payload.toEmail }}</td>
                <td>{{ email.payload.subject || email.payload.eventTitle }}</td>
                <td>{{ email.payload.totalUnpaidFines || 'General' }}</td>
                <td>{{ formatRecordTime(email.createdAt) }}</td>
                <td>{{ formatRecordTime(email.lastTriedAt) }}</td>
                <td>{{ email.attempts || 0 }}</td>
                <td>{{ email.lastError || 'Waiting to retry' }}</td>
                <td>{{ email.payload.message || email.payload.customMessage || 'Default notice' }}</td>
              </tr>
              <tr v-if="!pendingEmails.length">
                <td colspan="9">No pending emails</td>
              </tr>
            </tbody>
          </table>
        </section>

        <section class="panel">
          <div class="panel-heading">
            <div>
              <h2>Sent Email History</h2>
              <span>{{ sentEmails.length }} delivered fine notice(s)</span>
            </div>
          </div>
          <table>
            <thead>
              <tr>
                <th>Student</th>
                <th>Email</th>
                <th>Subject / Event</th>
                <th>New Fine</th>
                <th>Total</th>
                <th>Sent</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="email in sentEmails" :key="email.id">
                <td>{{ email.payload.studentName }}</td>
                <td>{{ email.payload.toEmail }}</td>
                <td>{{ email.payload.subject || email.payload.eventTitle }}</td>
                <td>{{ email.payload.newFineAmount || '-' }}</td>
                <td>{{ email.payload.totalUnpaidFines || 'General' }}</td>
                <td>{{ formatRecordTime(email.sentAt) }}</td>
                <td>{{ email.payload.message || email.payload.customMessage || 'Default notice' }}</td>
              </tr>
              <tr v-if="!sentEmails.length">
                <td colspan="7">No sent fine emails yet</td>
              </tr>
            </tbody>
          </table>
        </section>
      </section>

      <section v-else-if="activeView === 'transactions'" class="view-stack">
        <section v-for="group in transactionGroups" :key="group.id" class="panel">
          <div class="panel-heading">
            <div>
              <h2>{{ group.title }}</h2>
              <span>{{ group.rows.length }} record(s)</span>
            </div>
            <strong>{{ money(group.total) }}</strong>
          </div>
          <table>
            <thead>
              <tr>
                <th>Reference</th>
                <th>Student / Person</th>
                <th>Details</th>
                <th>Amount</th>
                <th>Status</th>
                <th>Date</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in group.rows" :key="row.key">
                <td>{{ row.reference }}</td>
                <td>{{ row.person }}</td>
                <td>{{ row.details }}</td>
                <td>{{ money(row.amount) }}</td>
                <td><span class="badge" :class="{ paid: row.status === 'Paid' }">{{ row.status }}</span></td>
                <td>{{ row.date }}</td>
                <td class="table-actions">
                  <button v-if="row.source === 'collection'" type="button" @click="editCollection(row.record)">Edit</button>
                  <button v-if="row.source === 'collection'" type="button" @click="removeCollection(row.record.id)">Delete</button>
                  <button v-if="row.source === 'fine'" type="button" @click="editFine(row.record)">Edit</button>
                  <button v-if="row.source === 'fine'" type="button" @click="removeFine(row.record.id)">Delete</button>
                  <button v-if="row.source === 'expense'" type="button" @click="editDisbursement(row.record)">Edit</button>
                  <button v-if="row.source === 'expense'" type="button" @click="removeDisbursement(row.record.id)">Delete</button>
                </td>
              </tr>
              <tr v-if="!group.rows.length">
                <td colspan="7">No {{ group.title.toLowerCase() }} yet</td>
              </tr>
            </tbody>
          </table>
        </section>
      </section>

      <section v-else-if="activeView === 'reports'" class="view-stack">
        <div class="stat-grid">
          <article class="stat-card">
            <span>Total Collected</span>
            <strong>{{ money(totalCollections) }}</strong>
            <small>Paid receipts only</small>
          </article>
          <article class="stat-card">
            <span>Expenses</span>
            <strong>{{ money(totalDisbursements) }}</strong>
            <small>Department disbursements</small>
          </article>
          <article class="stat-card">
            <span>Unpaid Fines</span>
            <strong>{{ money(unpaidFines) }}</strong>
            <small>Outstanding balances</small>
          </article>
          <article class="stat-card">
            <span>Attendance Rate</span>
            <strong>{{ attendanceRate }}%</strong>
            <small>Present or late, excluding excused</small>
          </article>
        </div>

        <div class="split-grid">
          <section class="panel report-panel">
            <div class="panel-heading flat">
              <div>
                <h2>Report Summary</h2>
                <span>Ready for printing or CSV export</span>
              </div>
            </div>
            <dl class="report-list">
              <div>
                <dt>Active students</dt>
                <dd>{{ students.length }}</dd>
              </div>
              <div>
                <dt>Ledger entries</dt>
                <dd>{{ collections.length }}</dd>
              </div>
              <div>
                <dt>Fine records</dt>
                <dd>{{ fines.length }}</dd>
              </div>
              <div>
                <dt>Attendance entries</dt>
                <dd>{{ attendanceRecords.length }}</dd>
              </div>
            </dl>
            <div class="button-row">
              <button type="button" class="primary-action" @click="windowPrint">Print Report</button>
            </div>
          </section>

        </div>

      </section>

      <aside v-if="selectedStudent" class="student-drawer">
        <button type="button" class="drawer-close" @click="selectedStudentId = null">Close</button>
        <p class="eyebrow">Student Profile</p>
        <h2>{{ selectedStudent.name }}</h2>
        <p>{{ selectedStudent.studentNo }} - {{ selectedStudent.course }}</p>
        <figure class="qr-card">
          <img v-if="studentQrCodes[selectedStudent.studentNo]" :src="studentQrCodes[selectedStudent.studentNo]" :alt="`QR code for ${selectedStudent.name}`" />
          <figcaption>{{ qrPayload(selectedStudent.studentNo) }}</figcaption>
        </figure>
        <div class="mini-stats">
          <span>Balance <strong>{{ money(balanceFor(selectedStudent.id)) }}</strong></span>
          <span>Attendance <strong>{{ attendanceFor(selectedStudent.id) }}%</strong></span>
          <span>Contact <strong>{{ selectedStudent.contact || 'No contact' }}</strong></span>
          <span>RFID <strong>{{ selectedStudent.rfidUid || 'Not mapped' }}</strong></span>
        </div>
      </aside>
    </main>
  </div>
</template>

<script setup>
import jsQR from 'jsqr';
import QRCode from 'qrcode';
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue';

const storageKey = 'kier-records-v2';
const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '');
const requiredQrPayload = '1162304531';

const activeView = ref('dashboard');
const searchTerm = ref('');
const selectedStudentId = ref(null);
const editingStudentId = ref(null);
const toastMessage = ref('');
const health = ref(null);
const healthError = ref('');
const loginOpen = ref(false);
const authToken = ref(localStorage.getItem('kier-auth-token') || '');
const authUser = reactive({ username: localStorage.getItem('kier-auth-username') || 'Guest', role: localStorage.getItem('kier-auth-role') || 'Guest' });
const loginForm = reactive({ username: 'admin', password: 'Admin123!' });
const scannerVideo = ref(null);
const quickScanInput = ref(null);
const scannerActive = ref(false);
const scannerMessage = ref('Camera scanner is ready. You can also type the QR value manually.');
const quickScanValue = ref('');
const manualQr = ref('');
const manualRfid = ref('');
const studentQrCodes = ref({});
const scanPop = reactive({
  visible: false,
  name: '',
  studentNo: '',
  method: '',
  status: '',
  event: '',
  time: '',
});
let scannerStream = null;
let scannerTimer = null;
let scanPopTimer = null;
let emailRetryTimer = null;
let lastScan = { value: '', time: 0 };
const scanCooldownMs = 750;

const students = ref([]);
const collections = ref([]);
const fines = ref([]);
const attendanceRecords = ref([]);
const disbursements = ref([]);
const activity = ref([]);
const pendingEmails = ref([]);
const sentEmails = ref([]);
const emailComposer = reactive({
  mode: 'general',
  studentId: null,
  subject: 'Payment reminder',
  message: '',
});

const navItems = [
  { id: 'dashboard', label: 'Dashboard', short: 'DB' },
  { id: 'students', label: 'Students', short: 'ST' },
  { id: 'collections', label: 'Collections', short: 'CO' },
  { id: 'fines', label: 'Fines', short: 'FI' },
  { id: 'attendance', label: 'Attendance', short: 'AT' },
  { id: 'admin', label: 'Admin', short: 'AD' },
  { id: 'emails', label: 'Emails', short: 'EM' },
  { id: 'transactions', label: 'Transactions', short: 'TR' },
  { id: 'reports', label: 'Reports', short: 'RP' },
];

const sections = {
  dashboard: { eyebrow: 'Overview', title: 'Department records dashboard', action: 'Add Receipt' },
  students: { eyebrow: 'Directory', title: 'Students and balances', action: 'Add Student' },
  collections: { eyebrow: 'Ledger', title: 'Collections and receipts', action: 'Add Receipt' },
  fines: { eyebrow: 'Register', title: 'Fines and payment status', action: 'Add Fine' },
  admin: { eyebrow: 'Admin', title: 'Student billing and edits', action: 'Admin Actions' },
  attendance: { eyebrow: 'Events', title: 'Attendance monitoring', action: 'Record Attendance' },
  emails: { eyebrow: 'Outbox', title: 'Email delivery tracking', action: 'Send Pending' },
  transactions: { eyebrow: 'Ledger', title: 'All transactions', action: 'Open Reports' },
  reports: { eyebrow: 'Summary', title: 'Financial and activity reports', action: 'Print' },
};

const studentForm = reactive(blankStudent());
const collectionForm = reactive({ studentId: 1, category: '', amount: '', receipt: '', status: 'Paid' });
const editingCollectionId = ref(null);
const payingFineId = ref(null);
const payingFineIds = ref([]);
const collectionStudentName = ref('');
const receiptEmail = ref('');
const collectionCategories = ref(['Department Fee', 'Event Contribution', 'Fine Payment', 'Fundraising']);
const studentSortField = ref('name');
const studentSortDirection = ref('asc');
const studentGroupBy = ref('none');
const fineForm = reactive({ studentId: 1, category: '', amount: '', status: 'Unpaid' });
const editingFineId = ref(null);
const fineStudentName = ref('');
const selectedAbsentFineStudentId = ref(null);
const attendanceForm = reactive({ event: '', studentId: 1, status: 'Present' });
const scanForm = reactive({
  eventTitle: '',
  sessionType: 'Log In',
  status: 'Present',
  openTime: dateTimeLocalOffset(0).slice(11),
  closeTime: dateTimeLocalOffset(60).slice(11),
  absentFine: 0,
  finePerLateMinute: 1,
  maxLateFine: 50,
});
const attendanceEvents = ref([]);
const currentAttendanceEventId = ref(null);
const disbursementForm = reactive({ description: '', usedBy: '', amount: '' });
const editingDisbursementId = ref(null);

const activeSection = computed(() => sections[activeView.value] || sections.dashboard);
const isLoggedIn = computed(() => Boolean(authToken.value));
const canAccessAdmin = computed(() => isLoggedIn.value && ['Administrator', 'Treasurer', 'Officer'].includes(authUser.role));
const apiOnline = computed(() => health.value?.status === 'Running');
const healthDetail = computed(() => {
  if (health.value) {
    return `Database: ${health.value.database}`;
  }

  return healthError.value || 'Saved locally in this browser';
});
const selectedStudent = computed(() => students.value.find((student) => student.id === selectedStudentId.value));
const studentsWithEmail = computed(() => students.value.filter((student) => String(student.email || '').trim()));
const activeAttendanceEvent = computed(() => attendanceEvents.value.find((event) => event.id === currentAttendanceEventId.value));
const normalizedSearch = computed(() => searchTerm.value.trim().toLowerCase());
const totalCollections = computed(() =>
  collections.value.filter((item) => item.status === 'Paid').reduce((sum, item) => sum + Number(item.amount), 0),
);
const totalDisbursements = computed(() => disbursements.value.reduce((sum, item) => sum + Number(item.amount), 0));
const outstandingBills = computed(() =>
  collections.value.filter((item) => item.status !== 'Paid').reduce((sum, item) => sum + Number(item.amount), 0),
);
const availableFunds = computed(() => totalCollections.value - totalDisbursements.value);
const unpaidFines = computed(() =>
  fines.value.filter((fine) => fine.status !== 'Paid').reduce((sum, item) => sum + Number(item.amount), 0),
);
const attendanceRate = computed(() => {
  const countedRecords = attendanceRecords.value.filter((record) => record.status !== 'Excused');
  if (!countedRecords.length) {
    return 0;
  }

  const counted = countedRecords.filter((record) => ['Present', 'Late'].includes(record.status)).length;
  return Math.round((counted / countedRecords.length) * 100);
});
const stats = computed(() => [
  { label: 'Available Funds', value: money(availableFunds.value), detail: 'Collections minus expenses' },
  { label: 'Collected', value: money(totalCollections.value), detail: `${collections.value.filter((collection) => collection.status === 'Paid').length} receipts` },
  { label: 'Outstanding Bills', value: money(outstandingBills.value), detail: `${collections.value.filter((collection) => collection.status !== 'Paid').length} open bills` },
  { label: 'Unpaid Fines', value: money(unpaidFines.value), detail: `${fines.value.filter((fine) => fine.status !== 'Paid').length} open fines` },
  { label: 'Attendance', value: `${attendanceRate.value}%`, detail: 'Excused records are neutral' },
]);
const totalOwed = computed(() => outstandingBills.value + unpaidFines.value);
const studentOwed = computed(() =>
  students.value
    .map((student) => {
      const unpaidBills = collections.value
        .filter((collection) => collection.studentId === student.id && collection.status !== 'Paid')
        .reduce((sum, item) => sum + Number(item.amount), 0);
      const unpaidFineAmount = fines.value
        .filter((fine) => fine.studentId === student.id && fine.status !== 'Paid')
        .reduce((sum, item) => sum + Number(item.amount), 0);

      return {
        ...student,
        unpaidBills,
        unpaidFineAmount,
        totalDue: unpaidBills + unpaidFineAmount,
      };
    })
    .sort((a, b) => b.totalDue - a.totalDue),
);
const priorityStudents = computed(() =>
  students.value
    .map((student) => ({
      ...student,
      balance: balanceFor(student.id),
      attendance: attendanceFor(student.id),
    }))
    .sort((a, b) => b.balance - a.balance || a.attendance - b.attendance)
    .slice(0, 5),
);
const collectionNameSuggestions = computed(() => {
  const term = String(collectionStudentName.value || '').trim().toLowerCase();
  if (!term) {
    return [];
  }

  const matches = students.value
    .filter((student) => String(student.name || '').toLowerCase().includes(term))
    .slice(0, 6)
    .map((student) => student.name);

  return matches.some((name) => name.toLowerCase() === term) ? [] : matches;
});
const fineNameSuggestions = computed(() => {
  const term = String(fineStudentName.value || '').trim().toLowerCase();
  if (!term) {
    return [];
  }

  const matches = students.value
    .filter((student) => String(student.name || '').toLowerCase().includes(term))
    .slice(0, 6)
    .map((student) => student.name);

  return matches.some((name) => name.toLowerCase() === term) ? [] : matches;
});
const cashFlow = computed(() => {
  const months = ['Aug', 'Sep', 'Oct', 'Nov'];
  const max = Math.max(totalCollections.value, totalDisbursements.value, 1);

  return months.map((month) => {
    const income = collections.value.filter((item) => item.month === month).reduce((sum, item) => sum + Number(item.amount), 0);
    const expense = disbursements.value.filter((item) => item.month === month).reduce((sum, item) => sum + Number(item.amount), 0);

    return {
      month,
      net: income - expense,
      incomeWidth: Math.max(4, Math.round((income / max) * 100)),
      expenseWidth: Math.max(4, Math.round((expense / max) * 100)),
    };
  });
});
const filteredStudents = computed(() => filterBy(students.value, (student) => [student.studentNo, student.name, student.course]));
const sortedStudents = computed(() => {
  const list = [...filteredStudents.value];
  const direction = studentSortDirection.value === 'asc' ? 1 : -1;

  return list.sort((a, b) => {
    const field = studentSortField.value;
    const getValue = (student) => {
      if (field === 'balance') {
        return balanceFor(student.id);
      }

      return String(student[field] ?? '').toLowerCase();
    };

    const valueA = getValue(a);
    const valueB = getValue(b);

    if (typeof valueA === 'number' && typeof valueB === 'number') {
      return direction * (valueA - valueB);
    }

    return direction * String(valueA).localeCompare(String(valueB), undefined, { numeric: true });
  });
});
const groupedStudents = computed(() => {
  if (studentGroupBy.value === 'none') {
    return [];
  }

  const groups = new Map();
  const keyFn = studentGroupBy.value === 'course' ? (student) => student.course || 'Unknown' : (student) => 'General';

  for (const student of sortedStudents.value) {
    const groupKey = keyFn(student);
    if (!groups.has(groupKey)) {
      groups.set(groupKey, []);
    }
    groups.get(groupKey).push(student);
  }

  return Array.from(groups.entries())
    .sort(([a], [b]) => String(a).localeCompare(String(b), undefined, { numeric: true }))
    .map(([group, items]) => ({ group, items }));
});
const filteredCollections = computed(() =>
  filterBy(collections.value, (collection) => [collection.receipt, collection.category, studentName(collection.studentId)]),
);
const filteredFines = computed(() => filterBy(fines.value, (fine) => [fine.category, fine.status, studentName(fine.studentId)]));
const filteredAttendance = computed(() =>
  filterBy(attendanceRecords.value, (record) => [record.event, record.status, studentName(record.studentId)]),
);
const selectedAbsentFineStudent = computed(() =>
  students.value.find((student) => student.id === selectedAbsentFineStudentId.value),
);
const selectedStudentAbsentFines = computed(() =>
  selectedAbsentFineStudentId.value ? absentFineRowsFor(selectedAbsentFineStudentId.value) : [],
);
const selectedStudentAbsentFineTotal = computed(() =>
  selectedStudentAbsentFines.value.reduce((sum, item) => sum + Number(item.amount || 0), 0),
);
const selectedStudentUnpaidAbsentFineTotal = computed(() =>
  selectedStudentAbsentFines.value
    .filter((item) => item.fineId && item.status !== 'Paid')
    .reduce((sum, item) => sum + Number(item.amount || 0), 0),
);
const transactionRows = computed(() => {
  const collectionRows = collections.value.map((collection) => ({
    key: `collection-${collection.id}`,
    source: 'collection',
    record: collection,
    type: collection.status === 'Paid' ? 'Receipt' : 'Bill',
    reference: collection.receipt || '-',
    person: studentName(collection.studentId),
    details: collection.category,
    amount: Number(collection.amount || 0),
    status: collection.status || 'Paid',
    date: collection.month || '-',
  }));
  const fineRows = fines.value.map((fine) => ({
    key: `fine-${fine.id}`,
    source: 'fine',
    record: fine,
    type: 'Fine',
    reference: `FINE-${fine.id}`,
    person: studentName(fine.studentId),
    details: fine.category,
    amount: Number(fine.amount || 0),
    status: fine.status || 'Unpaid',
    date: '-',
  }));
  const expenseRows = disbursements.value.map((expense) => ({
    key: `expense-${expense.id}`,
    source: 'expense',
    record: expense,
    type: 'Fund / Expense',
    reference: `EXP-${expense.id}`,
    person: expense.usedBy || 'Not specified',
    details: expense.description,
    amount: Number(expense.amount || 0),
    status: 'Withdrawn',
    date: expense.month || '-',
  }));

  return [...collectionRows, ...fineRows, ...expenseRows].sort((a, b) => String(b.key).localeCompare(String(a.key), undefined, { numeric: true }));
});
const transactionGroups = computed(() => {
  const collectionRows = transactionRows.value.filter((row) => row.source === 'collection');
  const fineRows = transactionRows.value.filter((row) => row.source === 'fine');
  const expenseRows = transactionRows.value.filter((row) => row.source === 'expense');
  const totalFor = (rows) => rows.reduce((sum, row) => sum + Number(row.amount || 0), 0);

  return [
    { id: 'collections', title: 'Bills / Receipts', rows: collectionRows, total: totalFor(collectionRows) },
    { id: 'fines', title: 'Fines', rows: fineRows, total: totalFor(fineRows) },
    { id: 'funds', title: 'Fund / Expenses', rows: expenseRows, total: totalFor(expenseRows) },
  ];
});

watch(
  [
    students,
    collections,
    fines,
    attendanceRecords,
    attendanceEvents,
    currentAttendanceEventId,
    disbursements,
    activity,
    pendingEmails,
    sentEmails,
  ],
  saveState,
  { deep: true },
);
watch(students, generateStudentQrCodes, { deep: true });

onMounted(async () => {
  loadState();
  reconcileFinePayments();
  selectedStudentId.value = null;
  resetStudentForm();
  await generateStudentQrCodes();

  try {
    const response = await apiFetch('/api/health');
    if (!response.ok) {
      throw new Error(`Health check failed with ${response.status}`);
    }

    health.value = await response.json();
  } catch (error) {
    healthError.value = error instanceof Error ? error.message : 'Backend not reachable';
  }

  if (isLoggedIn.value) {
    await initializeAuth();
  }

  await processPendingEmails({ silent: true });
  window.addEventListener('online', processPendingEmails);
  emailRetryTimer = window.setInterval(() => processPendingEmails({ silent: true }), 30000);
});

onBeforeUnmount(() => {
  stopQrScanner();
  window.removeEventListener('online', processPendingEmails);
  if (emailRetryTimer) {
    window.clearInterval(emailRetryTimer);
    emailRetryTimer = null;
  }
  if (scanPopTimer) {
    window.clearTimeout(scanPopTimer);
  }
});

function copy(value) {
  return JSON.parse(JSON.stringify(value));
}

function apiUrl(path) {
  return `${apiBaseUrl}${path}`;
}

function apiFetch(path, options = {}) {
  const headers = {
    ...(options.headers || {}),
  };

  if (options.body != null && !('Content-Type' in headers)) {
    headers['Content-Type'] = 'application/json';
  }

  if (authToken.value) {
    headers.Authorization = `Bearer ${authToken.value}`;
  }

  const init = {
    ...options,
    headers,
  };

  return fetch(apiUrl(path), init);
}

async function loadBackendData() {
  if (!isLoggedIn.value) {
    return;
  }

  try {
    const [studentsResponse, collectionsResponse, finesResponse, attendanceResponse, attendanceEventsResponse] = await Promise.all([
      apiFetch('/api/students'),
      apiFetch('/api/collections'),
      apiFetch('/api/fines'),
      apiFetch('/api/attendance'),
      apiFetch('/api/attendance/events'),
    ]);

    if (studentsResponse.ok) {
      students.value = await studentsResponse.json();
    }

    if (collectionsResponse.ok) {
      collections.value = await collectionsResponse.json();
    }

    if (finesResponse.ok) {
      fines.value = await finesResponse.json();
    }

    if (attendanceResponse.ok) {
      attendanceRecords.value = await attendanceResponse.json();
    }

    if (attendanceEventsResponse.ok) {
      const loadedEvents = await attendanceEventsResponse.json();
      attendanceEvents.value = Array.isArray(loadedEvents)
        ? loadedEvents.map((event) => ({
            id: event.id,
            title: event.title,
            eventDate: event.eventDate,
            location: event.location,
            description: event.description,
          }))
        : [];
    }

    reconcileFinePayments();
    notify('Backend data synced');
  } catch (error) {
    console.warn('Backend data load failed:', error);
  }
}

async function initializeAuth() {
  if (!authToken.value) {
    return;
  }

  try {
    const response = await apiFetch('/api/auth/me');
    if (!response.ok) {
      throw new Error('Invalid token');
    }

    const result = await response.json();
    authUser.username = result.username || authUser.username;
    authUser.role = result.role || authUser.role;
    localStorage.setItem('kier-auth-username', authUser.username);
    localStorage.setItem('kier-auth-role', authUser.role);

    await loadBackendData();
  } catch {
    authToken.value = '';
    authUser.username = 'Guest';
    authUser.role = 'Guest';
    localStorage.removeItem('kier-auth-token');
    localStorage.removeItem('kier-auth-username');
    localStorage.removeItem('kier-auth-role');
  }
}

function blankStudent() {
  return { studentNo: '', firstName: '', lastName: '', suffix: '', name: '', course: '', yearLevel: '', contact: '', email: '', rfidUid: '' };
}

function splitStudentName(name) {
  const parts = String(name || '').trim().split(/\s+/).filter(Boolean);
  if (!parts.length) {
    return { firstName: '', lastName: '' };
  }

  const suffix = extractSuffix(parts);
  return {
    firstName: parts[0],
    lastName: parts.slice(1).join(' '),
    suffix,
  };
}

function extractSuffix(parts) {
  const suffixPattern = /^(jr\.?|sr\.?|i{2,4}|v|vi|vii|viii|ix|x)$/i;
  const lastPart = parts.at(-1) || '';
  if (!suffixPattern.test(lastPart)) {
    return '';
  }

  parts.pop();
  return lastPart;
}

function normalizeStudentRecord(student) {
  const fallbackName = splitStudentName(student.name);
  const firstName = String(student.firstName || fallbackName.firstName || '').trim();
  const lastNameParts = String(student.lastName || fallbackName.lastName || '').trim().split(/\s+/).filter(Boolean);
  const detectedSuffix = extractSuffix(lastNameParts);
  const lastName = lastNameParts.join(' ');
  const suffix = String(student.suffix || fallbackName.suffix || detectedSuffix || '').trim();

  return {
    ...student,
    firstName,
    lastName,
    suffix,
    name: [firstName, lastName, suffix].filter(Boolean).join(' ') || String(student.name || '').trim(),
    rfidUid: student.rfidUid || '',
  };
}

function studentFromForm(id = null) {
  const firstName = String(studentForm.firstName || '').trim();
  const lastName = String(studentForm.lastName || '').trim();
  const suffix = String(studentForm.suffix || '').trim();
  return {
    ...(id ? { id } : {}),
    ...studentForm,
    firstName,
    lastName,
    suffix,
    name: [firstName, lastName, suffix].filter(Boolean).join(' '),
  };
}

function backendLastName(student) {
  return [student.lastName, student.suffix].filter(Boolean).join(' ').trim();
}

function setView(view) {
  if (view === 'admin' && !canAccessAdmin.value) {
    notify('Please sign in with an administrator account to open admin tools.');
    openLogin();
    return;
  }

  activeView.value = view;
  searchTerm.value = '';
  selectedStudentId.value = null;
}

function openLogin() {
  loginOpen.value = true;
}

async function login() {
  try {
    const response = await apiFetch('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({
        username: loginForm.username,
        password: loginForm.password,
      }),
    });

    if (!response.ok) {
      const message = await responseMessage(response);
      notify(message || 'Login failed');
      return;
    }

    const result = await response.json();
    authToken.value = result.token || '';
    authUser.username = result.username || loginForm.username;
    authUser.role = result.role || 'Administrator';

    localStorage.setItem('kier-auth-token', authToken.value);
    localStorage.setItem('kier-auth-username', authUser.username);
    localStorage.setItem('kier-auth-role', authUser.role);

    loginOpen.value = false;
    loginForm.password = '';
    notify(`Signed in as ${authUser.username}`);

    await loadBackendData();
  } catch (error) {
    notify(error instanceof Error ? error.message : 'Unable to sign in');
  }
}

function logout() {
  authToken.value = '';
  authUser.username = 'Guest';
  authUser.role = 'Guest';
  localStorage.removeItem('kier-auth-token');
  localStorage.removeItem('kier-auth-username');
  localStorage.removeItem('kier-auth-role');
  notify('Signed out');
  setView('dashboard');
}

function selectStudent(studentId) {
  selectedStudentId.value = studentId;
}

function goToCollectionsForStudent(studentId) {
  const student = students.value.find((item) => item.id === studentId);
  if (!student) {
    return;
  }

  collectionStudentName.value = student.name;
  receiptEmail.value = student.email || '';
  payingFineId.value = null;
  payingFineIds.value = [];
  activeView.value = 'collections';
}

function goToFinesForStudent(studentId) {
  const student = students.value.find((item) => item.id === studentId);
  if (!student) {
    return;
  }

  fineStudentName.value = student.name;
  activeView.value = 'fines';
}

function openAdminAddReceipt(studentId) {
  const student = students.value.find((item) => item.id === studentId);
  if (!student) return;

  // Prefill admin quick-action collection form
  collectionStudentName.value = student.name;
  collectionForm.studentId = student.id;
  collectionForm.category = 'Event Contribution';
  collectionForm.amount = '';
  collectionForm.status = 'Unpaid';
  editingCollectionId.value = null;
  payingFineId.value = null;
  payingFineIds.value = [];
  // ensure we're on admin view
  activeView.value = 'admin';
}

function openAdminAddFine(studentId) {
  const student = students.value.find((item) => item.id === studentId);
  if (!student) return;

  fineStudentName.value = student.name;
  fineForm.studentId = student.id;
  fineForm.category = 'General Fine';
  fineForm.amount = '';
  fineForm.status = 'Unpaid';
  editingFineId.value = null;
  activeView.value = 'admin';
}

function seedSampleStudent() {
  const exists = students.value.find((s) => s.name === 'KIER LANAYON');
  if (exists) {
    notify('Sample student already exists');
    return;
  }

  const student = {
    id: nextId(students.value),
    studentNo: String(1000 + nextId(students.value)),
    firstName: 'KIER',
    lastName: 'LANAYON',
    suffix: '',
    name: 'KIER LANAYON',
    course: 'BSCS',
    yearLevel: '1',
    contact: '',
    email: '',
    rfidUid: '',
  };
  students.value.unshift(student);
  notify('Sample student created');
}

function qrPayload(studentNo) {
  return requiredQrPayload;
}

function parseQrPayload(value) {
  const cleaned = String(value || '').trim();
  if (!cleaned) {
    return '';
  }

  return cleaned.toUpperCase().startsWith('KIER:') ? cleaned.slice(5).trim() : cleaned;
}

function normalizeRfid(value) {
  return String(value || '').trim().replaceAll(' ', '').toUpperCase();
}

async function generateStudentQrCodes() {
  const nextCodes = {};

  for (const student of students.value) {
    nextCodes[student.studentNo] = await QRCode.toDataURL(qrPayload(student.studentNo), {
      width: 240,
      margin: 2,
      color: {
        dark: '#142027',
        light: '#ffffff',
      },
    });
  }

  studentQrCodes.value = nextCodes;
}

function filterBy(records, valuesFor) {
  const list = Array.isArray(records) ? records : [];
  if (!normalizedSearch.value) {
    return list;
  }

  return list.filter((record) =>
    valuesFor(record).some((value) => String(value).toLowerCase().includes(normalizedSearch.value)),
  );
}

function studentName(studentId) {
  return students.value.find((student) => student.id === studentId)?.name || 'Unknown student';
}

function studentByName(name) {
  const normalized = String(name || '').trim().toLowerCase();
  if (!normalized) {
    return null;
  }

  return students.value.find((student) => String(student.name || '').toLowerCase().includes(normalized)) || null;
}

function handleCollectionStudentNameInput() {
  const term = String(collectionStudentName.value || '').trim();
  if (!term) {
    return;
  }

  const matches = students.value.filter((student) => String(student.name || '').toLowerCase().includes(term.toLowerCase()));
  if (matches.length === 1) {
    collectionStudentName.value = matches[0].name;
    if (!receiptEmail.value && matches[0].email) {
      receiptEmail.value = matches[0].email;
    }
  }
}

function selectCollectionStudentName(name) {
  collectionStudentName.value = name;
  const matchedStudent = studentByName(name);
  if (!receiptEmail.value && matchedStudent?.email) {
    receiptEmail.value = matchedStudent.email;
  }
}

function handleFineStudentNameInput() {
  // Keep typing fully manual. Suggestions are applied only when clicked.
}

function selectFineStudentName(name) {
  fineStudentName.value = name;
}

function balanceFor(studentId) {
  const openFines = fines.value
    .filter((fine) => fine.studentId === studentId && fine.status !== 'Paid')
    .reduce((sum, fine) => sum + Number(fine.amount), 0);
  const finePayments = collections.value
    .filter((collection) => collection.studentId === studentId && collection.category === 'Fine Payment' && collection.status === 'Paid')
    .reduce((sum, collection) => sum + Number(collection.amount), 0);
  const unpaidBills = collections.value
    .filter((collection) => collection.studentId === studentId && collection.status !== 'Paid')
    .reduce((sum, collection) => sum + Number(collection.amount), 0);

  return Math.max(openFines + unpaidBills - finePayments, 0);
}

function attendanceFor(studentId) {
  const records = attendanceRecords.value.filter((record) => record.studentId === studentId && record.status !== 'Excused');
  if (!records.length) {
    return 0;
  }

  const counted = records.filter((record) => ['Present', 'Late'].includes(record.status)).length;
  return Math.round((counted / records.length) * 100);
}

function absentFineRowsFor(studentId) {
  const absentRecords = attendanceRecords.value.filter(
    (record) => record.studentId === studentId && String(record.status || '').toLowerCase() === 'absent',
  );
  const absentFines = fines.value.filter(
    (fine) => fine.studentId === studentId && String(fine.category || '').toLowerCase().startsWith('absent - '),
  );
  const rows = absentRecords.map((record) => {
    const matchingFine = absentFines.find((fine) => String(fine.category || '').includes(record.event));
    return {
      key: `record-${record.id}`,
      fineId: matchingFine?.id || null,
      event: record.event,
      recordedAt: record.recordedAt,
      amount: Number(matchingFine?.amount || 0),
      status: matchingFine?.status || 'No fine',
    };
  });

  for (const fine of absentFines) {
    const alreadyListed = rows.some((row) => String(fine.category || '').includes(row.event));
    if (alreadyListed) {
      continue;
    }

    rows.push({
      key: `fine-${fine.id}`,
      fineId: fine.id,
      event: String(fine.category || '').replace(/^Absent -\s*/i, ''),
      recordedAt: null,
      amount: Number(fine.amount || 0),
      status: fine.status,
    });
  }

  return rows.sort((a, b) => String(a.event).localeCompare(String(b.event), undefined, { numeric: true }));
}

function absentFineCountFor(studentId) {
  return absentFineRowsFor(studentId).length;
}

function money(value) {
  return new Intl.NumberFormat('en-PH', {
    style: 'currency',
    currency: 'PHP',
    maximumFractionDigits: 0,
  }).format(value);
}

function nextId(records) {
  return Math.max(0, ...records.map((item) => Number(item.id))) + 1;
}

function nextReference(prefix) {
  const existingNumbers = collections.value
    .map((item) => item.receipt)
    .filter((receipt) => typeof receipt === 'string' && receipt.startsWith(`${prefix}-`))
    .map((receipt) => Number(receipt.split('-')[1]))
    .filter((number) => Number.isFinite(number));

  if (!existingNumbers.length) {
    return 1001;
  }

  return Math.max(...existingNumbers) + 1;
}

function nextReceipt() {
  return `RC-${nextReference('RC')}`;
}

function currentMonth() {
  return new Date().toLocaleString('en-US', { month: 'short' });
}

function formatRecordTime(value) {
  if (!value) {
    return 'Not tracked';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return 'Not tracked';
  }

  return date.toLocaleString([], {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function dateTimeLocalOffset(minutes) {
  const date = new Date(Date.now() + minutes * 60 * 1000);
  date.setSeconds(0, 0);
  return new Date(date.getTime() - date.getTimezoneOffset() * 60 * 1000).toISOString().slice(0, 16);
}

function formatDateTimeLocal(date) {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60 * 1000);
  return local.toISOString().slice(0, 16);
}

function eventTimeValue(value) {
  if (!value) {
    return '';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return String(value).slice(11, 16);
  }

  return formatDateTimeLocal(date).slice(11);
}

function timeStringToDate(time, baseDate = new Date()) {
  const [hours, minutes] = String(time || '').split(':').map(Number);
  const date = new Date(baseDate);
  if (Number.isInteger(hours) && Number.isInteger(minutes)) {
    date.setHours(hours, minutes, 0, 0);
  }
  return date;
}

function attendanceWindowPayload() {
  const activeEvent = activeAttendanceEvent.value;

  return {
    openAt: activeEvent?.openAt || null,
    lateAt: activeEvent?.lateAt || activeEvent?.closeAt || null,
    closeAt: null,
    finePerLateMinute: Number(scanForm.finePerLateMinute || 0),
    maxLateFine: Number(scanForm.maxLateFine || 0),
  };
}

function notify(message) {
  toastMessage.value = message;
  window.clearTimeout(notify.timer);
  notify.timer = window.setTimeout(() => {
    toastMessage.value = '';
  }, 2400);
}

function logActivity(type, title, detail) {
  activity.value.unshift({ id: Date.now(), type, title, detail });
}

async function saveStudent() {
  const formStudent = studentFromForm(editingStudentId.value);
  if (editingStudentId.value) {
    const index = students.value.findIndex((student) => student.id === editingStudentId.value);
    if (index >= 0) {
      students.value[index] = { ...students.value[index], ...formStudent };
      await updateBackendStudent(students.value[index]);
      logActivity('Student', `${formStudent.name} updated`, 'Student profile was changed');
      notify('Student updated');
    }
  } else {
    const student = studentFromForm(nextId(students.value));
    students.value.unshift(student);
    await createBackendStudent(student);
    selectedStudentId.value = student.id;
    logActivity('Student', `${student.name} added`, 'New student record created');

    if (student.email) {
      const qrEmailSent = await sendStudentQrEmail(student);
      if (qrEmailSent) {
        notify('Student saved and QR code email sent');
      }
    } else {
      notify('Student saved. No QR email was sent because no email address was provided.');
    }
  }

  resetStudentForm();
}

async function sendStudentQrEmail(student) {
  const qrDataUrl = studentQrCodes.value[student.studentNo];
  if (!student.email || !qrDataUrl) {
    return false;
  }

  try {
    const response = await apiFetch('/api/email/student-qr', {
      method: 'POST',
      body: JSON.stringify({
        toEmail: student.email,
        studentName: student.name,
        studentNo: student.studentNo,
        course: student.course,
        qrImageBase64: qrDataUrl,
      }),
    });

    if (!response.ok) {
      let result = null;
      let details = '';
      try {
        result = await response.json();
      } catch {
        details = await response.text();
      }

      const message = result?.message || response.statusText || 'Email send failed';
      const errorDetail = result?.error || details || '';
      notify(
        `Student saved, but QR code email could not be sent: ${message}${errorDetail ? ` (${errorDetail})` : ''}`,
      );
      return false;
    }

    return true;
  } catch (error) {
    notify(`Student saved, but QR code email could not be sent: ${error instanceof Error ? error.message : String(error)}`);
    return false;
  }
}

async function createBackendStudent(student) {
  try {
    await apiFetch('/api/students', {
      method: 'POST',
      body: JSON.stringify({
        studentNo: student.studentNo,
        firstName: student.firstName || student.name,
        lastName: backendLastName(student),
        course: student.course,
        yearLevel: student.yearLevel,
        contactNumber: student.contact || '',
        email: student.email || '',
        rfidUid: normalizeRfid(student.rfidUid),
      }),
    });
  } catch {
    // The frontend still works offline; QR scans will sync once the backend has the student.
  }
}

async function updateBackendStudent(student) {
  try {
    await apiFetch(`/api/students/${student.id}`, {
      method: 'PUT',
      body: JSON.stringify({
        studentNo: student.studentNo,
        firstName: student.firstName || student.name,
        lastName: backendLastName(student),
        course: student.course,
        yearLevel: student.yearLevel,
        contactNumber: student.contact || '',
        email: student.email || '',
        rfidUid: normalizeRfid(student.rfidUid),
      }),
    });
  } catch {
    // Local edits stay available even when the backend is offline.
  }
}

function editStudent(student) {
  const normalizedStudent = normalizeStudentRecord(student);
  editingStudentId.value = student.id;
  Object.assign(studentForm, {
    studentNo: normalizedStudent.studentNo,
    firstName: normalizedStudent.firstName,
    lastName: normalizedStudent.lastName,
    suffix: normalizedStudent.suffix,
    name: normalizedStudent.name,
    course: normalizedStudent.course,
    yearLevel: normalizedStudent.yearLevel,
    contact: normalizedStudent.contact || '',
    email: normalizedStudent.email || '',
    rfidUid: normalizedStudent.rfidUid || '',
  });
}

function resetStudentForm() {
  editingStudentId.value = null;
  Object.assign(studentForm, blankStudent());
}

function removeStudent(studentId) {
  students.value = students.value.filter((student) => student.id !== studentId);
  collections.value = collections.value.filter((collection) => collection.studentId !== studentId);
  fines.value = fines.value.filter((fine) => fine.studentId !== studentId);
  attendanceRecords.value = attendanceRecords.value.filter((record) => record.studentId !== studentId);
  if (selectedStudentId.value === studentId) {
    selectedStudentId.value = null;
  }
  logActivity('Student', 'Student removed', 'Related sample records were also removed');
  notify('Student deleted');
}

async function addCollection() {
  const linkedFineIds = currentPayingFineIds();
  const linkedFineId = linkedFineIds[0] || null;
  const linkedFines = fines.value.filter((fine) => linkedFineIds.includes(fine.id));
  const category = receiptCategoryFor(collectionForm.category, linkedFines);
  const receiptDetails = receiptDetailsFor(linkedFines);

  if (category && !collectionCategories.value.includes(category)) {
    collectionCategories.value.push(category);
  }

  const receipt =
    collectionForm.receipt || (collectionForm.status === 'Unpaid' ? `BILL-${nextReference('BILL')}` : nextReceipt());
  const typedName = String(collectionStudentName.value || '').trim();
  const matchedStudent = studentByName(typedName);
  const resolvedStudentId = matchedStudent ? matchedStudent.id : collectionForm.studentId;

  if (!receiptEmail.value && matchedStudent?.email) {
    receiptEmail.value = matchedStudent.email;
  }

  if (editingCollectionId.value) {
    const existing = collections.value.find((c) => c.id === editingCollectionId.value);
    if (existing) {
      existing.receipt = receipt;
      existing.studentId = resolvedStudentId;
      existing.category = category;
      existing.amount = Number(collectionForm.amount || 0);
      existing.month = currentMonth();
      existing.status = collectionForm.status || existing.status;
      existing.fineId = existing.fineId || linkedFineId || null;
      existing.fineIds = existing.fineIds?.length ? existing.fineIds : linkedFineIds;
    }
  } else {
    collections.value.unshift({
      id: nextId(collections.value),
      receipt,
      studentId: resolvedStudentId,
      category,
      amount: Number(collectionForm.amount || 0),
      month: currentMonth(),
      status: collectionForm.status || 'Paid',
      fineId: linkedFineId || null,
      fineIds: linkedFineIds,
    });
  }

  syncLinkedFinePayments(linkedFineIds, collectionForm.status === 'Paid');
  payingFineId.value = null;
  payingFineIds.value = [];

  const shouldSendReceipt = Boolean(receiptEmail.value);
  if (shouldSendReceipt) {
    try {
      const response = await apiFetch('/api/email/receipt', {
        method: 'POST',
        body: JSON.stringify({
          toEmail: receiptEmail.value,
          receiptNumber: receipt,
          studentName: studentName(resolvedStudentId),
          category,
          details: receiptDetails,
          amount: money(collectionForm.amount || 0),
          date: new Date().toLocaleString(),
        }),
      });

      if (!response.ok) {
        let result = null;
        let details = '';
        try {
          result = await response.json();
        } catch {
          details = await response.text();
        }

        const message = result?.message || response.statusText || 'Email send failed';
        const errorDetail = result?.error || details || '';
        notify(
          `Collection saved, but email could not be sent: ${message}${errorDetail ? ` (${errorDetail})` : ''}`,
        );
        return;
      }
    } catch (error) {
      notify(`Collection saved, but email could not be sent: ${error instanceof Error ? error.message : String(error)}`);
      return;
    }
  }

  logActivity('Receipt', `${receipt} recorded`, `${studentName(resolvedStudentId)} paid ${money(collectionForm.amount)}`);
  collectionForm.category = '';
  collectionForm.amount = '';
  collectionForm.receipt = '';
  collectionForm.studentId = 1;
  collectionStudentName.value = '';
  receiptEmail.value = '';
  editingCollectionId.value = null;
  payingFineId.value = null;
  payingFineIds.value = [];
  notify(shouldSendReceipt ? 'Collection saved and receipt email sent' : 'Collection saved. No receipt email was sent because no recipient address was provided.');
}

function addCollectionForAllStudents() {
  const category = String(collectionForm.category || 'Uncategorized').trim() || 'Uncategorized';
  const amount = Number(collectionForm.amount || 0);
  const status = 'Unpaid';

  if (!students.value.length) {
    notify('No students found');
    return;
  }

  if (amount <= 0) {
    notify('Enter amount before creating for all students');
    return;
  }

  if (category && !collectionCategories.value.includes(category)) {
    collectionCategories.value.push(category);
  }

  for (const student of students.value) {
    const receipt = status === 'Unpaid' ? `BILL-${nextReference('BILL')}` : nextReceipt();
    collections.value.unshift({
      id: nextId(collections.value),
      receipt,
      studentId: student.id,
      category,
      amount,
      month: currentMonth(),
      status,
      fineId: null,
      fineIds: [],
    });
  }

  logActivity('Bill', `${category} created for all students`, `${students.value.length} student(s) - ${money(amount)} each`);
  collectionForm.category = '';
  collectionForm.amount = '';
  collectionForm.receipt = '';
  collectionForm.status = status;
  collectionStudentName.value = '';
  editingCollectionId.value = null;
  payingFineId.value = null;
  payingFineIds.value = [];
  notify(`${category} created for ${students.value.length} student(s)`);
}

function currentPayingFineIds() {
  if (payingFineIds.value.length) {
    return [...payingFineIds.value];
  }

  return payingFineId.value ? [payingFineId.value] : [];
}

function receiptCategoryFor(defaultCategory, linkedFines = []) {
  if (linkedFines.length === 1) {
    return linkedFines[0].category || 'Fine Payment';
  }

  if (linkedFines.length > 1) {
    return 'Fine Payment - Multiple Absent Events';
  }

  return String(defaultCategory || 'Uncategorized').trim() || 'Uncategorized';
}

function receiptDetailsFor(linkedFines = []) {
  if (!linkedFines.length) {
    return '';
  }

  return linkedFines
    .map((fine) => `${fine.category || 'Fine'} - ${money(fine.amount || 0)}`)
    .join('\n');
}

function collectionFineIds(collection) {
  if (!collection) {
    return [];
  }

  if (Array.isArray(collection.fineIds) && collection.fineIds.length) {
    return collection.fineIds;
  }

  return collection.fineId ? [collection.fineId] : [];
}

function syncLinkedFinePayments(fineIds, isPaid) {
  for (const fineId of fineIds) {
    syncLinkedFinePayment(fineId, isPaid);
  }
}

function syncLinkedFinePayment(fineId, isPaid) {
  if (!fineId) {
    return;
  }

  const fine = fines.value.find((item) => item.id === fineId);
  if (!fine) {
    return;
  }

  fine.status = isPaid ? 'Paid' : 'Unpaid';
  logActivity('Fine', `${fine.category} marked ${fine.status}`, studentName(fine.studentId));
}

function reconcileFinePayments() {
  collections.value.forEach((collection) => {
    if (collection.category !== 'Fine Payment') {
      return;
    }

    const linkedFineIds = collectionFineIds(collection);
    if (linkedFineIds.length) {
      syncLinkedFinePayments(linkedFineIds, collection.status === 'Paid');
      return;
    }

    if (collection.status !== 'Paid') {
      return;
    }

    const matchingFine = fines.value.find(
      (fine) =>
        fine.studentId === collection.studentId &&
        fine.status !== 'Paid' &&
        Number(fine.amount || 0) === Number(collection.amount || 0),
    );

    if (matchingFine) {
      collection.fineId = matchingFine.id;
      matchingFine.status = 'Paid';
    }
  });
}

function toggleCollectionStatus(collectionId) {
  const collection = collections.value.find((item) => item.id === collectionId);
  if (!collection) {
    return;
  }

  if (collection.status !== 'Paid' && String(collection.receipt).startsWith('BILL-')) {
    collection.status = 'Paid';
    collection.receipt = nextReceipt();
    syncLinkedFinePayments(collectionFineIds(collection), true);
    logActivity('Bill', `Bill paid and converted to receipt`, studentName(collection.studentId));
    notify('Bill paid and converted to receipt');
    return;
  }

  collection.status = collection.status === 'Paid' ? 'Unpaid' : 'Paid';
  syncLinkedFinePayments(collectionFineIds(collection), collection.status === 'Paid');
  logActivity('Bill', `${collection.category} marked ${collection.status}`, studentName(collection.studentId));
  notify(`${collection.status === 'Paid' ? 'Bill marked paid' : 'Bill marked unpaid'}`);
}

function editCollection(collection) {
  editingCollectionId.value = collection.id;
  payingFineId.value = collection.fineId || null;
  payingFineIds.value = collectionFineIds(collection);
  collectionForm.category = collection.category;
  collectionForm.amount = collection.amount;
  collectionForm.receipt = collection.receipt;
  collectionForm.status = collection.status || 'Paid';
  collectionForm.studentId = collection.studentId;
  collectionStudentName.value = studentName(collection.studentId);
  receiptEmail.value = '';
  setView('collections');
}

function removeCollection(collectionId) {
  const collection = collections.value.find((item) => item.id === collectionId);
  syncLinkedFinePayments(collectionFineIds(collection), false);
  collections.value = collections.value.filter((item) => item.id !== collectionId);
  logActivity('Receipt', 'Receipt removed', 'Collection entry deleted');
  notify('Collection deleted');
}

function addFine() {
  const typedName = String(fineStudentName.value || '').trim().toLowerCase();
  const matchedStudent = students.value.find((student) => student.name.toLowerCase() === typedName);
  const resolvedStudentId = matchedStudent ? matchedStudent.id : fineForm.studentId;

  if (editingFineId.value) {
    const existing = fines.value.find((f) => f.id === editingFineId.value);
    if (existing) {
      existing.studentId = resolvedStudentId;
      existing.category = fineForm.category;
      existing.amount = Number(fineForm.amount || 0);
      existing.status = fineForm.status;
    }
  } else {
    fines.value.unshift({
      id: nextId(fines.value),
      studentId: resolvedStudentId,
      category: fineForm.category,
      amount: Number(fineForm.amount || 0),
      status: fineForm.status,
    });
  }
  logActivity('Fine', `${fineForm.category} fine added`, `${studentName(resolvedStudentId)} - ${money(fineForm.amount)}`);
  fineForm.category = '';
  fineForm.amount = '';
  fineForm.status = 'Unpaid';
  fineForm.studentId = resolvedStudentId;
  fineStudentName.value = '';
  editingFineId.value = null;
  notify('Fine saved');
}

function toggleFine(fineId) {
  const fine = fines.value.find((item) => item.id === fineId);
  if (!fine) {
    return;
  }

  if (fine.status !== 'Paid') {
    prepareFinePayment(fine);
    return;
  }

  fine.status = fine.status === 'Paid' ? 'Unpaid' : 'Paid';
  logActivity('Fine', `${fine.category} marked ${fine.status}`, studentName(fine.studentId));
  notify(`Fine marked ${fine.status.toLowerCase()}`);
}

function prepareFinePayment(fine) {
  prepareFinePaymentGroup([fine], 'Fine payment form is ready');
}

function prepareFinePaymentGroup(fineGroup, message) {
  const payableFines = fineGroup.filter((fine) => fine && fine.status !== 'Paid');
  if (!payableFines.length) {
    notify('No unpaid fines selected');
    return;
  }

  const studentId = payableFines[0].studentId;
  const student = students.value.find((item) => item.id === studentId);

  collectionForm.studentId = studentId;
  collectionForm.category = receiptCategoryFor('Fine Payment', payableFines);
  collectionForm.amount = payableFines.reduce((sum, fine) => sum + Number(fine.amount || 0), 0);
  collectionForm.receipt = '';
  collectionForm.status = 'Paid';
  collectionStudentName.value = studentName(studentId);
  receiptEmail.value = student?.email || '';
  editingCollectionId.value = null;
  payingFineId.value = payableFines[0].id;
  payingFineIds.value = payableFines.map((fine) => fine.id);
  setView('collections');
  notify(message);
}

function payAbsentFine(fineId) {
  const fine = fines.value.find((item) => item.id === fineId);
  if (!fine) {
    notify('Fine not found');
    return;
  }

  prepareFinePaymentGroup([fine], 'Collection form is ready for this absent event');
}

function payAllAbsentFinesForStudent(studentId) {
  const unpaidAbsentFines = fines.value.filter(
    (fine) =>
      fine.studentId === studentId &&
      fine.status !== 'Paid' &&
      String(fine.category || '').toLowerCase().startsWith('absent - '),
  );

  if (!unpaidAbsentFines.length) {
    notify('No unpaid absent fines for this student');
    return;
  }

  prepareFinePaymentGroup(unpaidAbsentFines, 'Collection form is ready for total absent fines');
}

function editFine(fine) {
  editingFineId.value = fine.id;
  fineForm.category = fine.category;
  fineForm.amount = fine.amount;
  fineForm.status = fine.status;
  fineForm.studentId = fine.studentId;
  fineStudentName.value = studentName(fine.studentId);
  setView('fines');
}

function removeFine(fineId) {
  fines.value = fines.value.filter((fine) => fine.id !== fineId);
  logActivity('Fine', 'Fine removed', 'Fine record deleted');
  notify('Fine deleted');
}

async function addAttendance() {
  const student = students.value.find((item) => item.id === attendanceForm.studentId);
  if (student) {
    await recordQrScan(student.studentNo, { silent: true });
    return;
  }

  const activeEventTitle = (attendanceEvents.value.find((e) => e.id === currentAttendanceEventId.value)?.title) || attendanceForm.event;
  addAttendanceLocal(activeEventTitle, attendanceForm.studentId, attendanceForm.status);
}

function addAttendanceLocal(event, studentId, status, recordedAt = new Date().toISOString()) {
  const existingIndex = attendanceRecords.value.findIndex(
    (record) => record.event === event && record.studentId === studentId,
  );

  if (existingIndex >= 0) {
    return { created: false, record: attendanceRecords.value[existingIndex] };
  }

  const record = {
    id: nextId(attendanceRecords.value),
    event,
    studentId,
    status,
    recordedAt,
  };
  attendanceRecords.value.unshift(record);
  logActivity('Attendance', `${event} recorded`, `${studentName(studentId)} - ${status}`);
  notify('Attendance saved');
  return { created: true, record };
}

function findAttendanceRecord(event, studentId) {
  return attendanceRecords.value.find((record) => record.event === event && record.studentId === studentId);
}

function showAlreadyRecorded(student, event, record, method) {
  const firstTime = formatRecordTime(record?.recordedAt);
  scannerMessage.value = `${student.name} already recorded for ${event}${firstTime ? ` at ${firstTime}` : ''}. First attendance kept.`;
  showScanPop(student, 'Already recorded', event, method);
  notify(`${student.name} already recorded`);
}

function createAttendanceEvent(title) {
  const typedName = String(title || scanForm.eventTitle || '').trim();
  if (!typedName) {
    notify('Enter event name before starting attendance');
    return null;
  }
  const existing = attendanceEvents.value.find((ev) => ev.title.toLowerCase() === typedName.toLowerCase());
  if (existing) {
    currentAttendanceEventId.value = existing.id;
    scanForm.eventTitle = existing.title;
    selectAttendanceEvent(existing.id);
    notify(`Event selected: ${existing.title}`);
    return existing;
  }
  const startDate = timeStringToDate(scanForm.openTime);
  const closeDate = scanForm.closeTime ? timeStringToDate(scanForm.closeTime, startDate) : new Date(startDate.getTime() + 60 * 60 * 1000);
  const ev = {
    id: nextId(attendanceEvents.value),
    title: typedName,
    sessionType: scanForm.sessionType,
    absentFine: Number(scanForm.absentFine || 0),
    createdAt: new Date().toISOString(),
    openAt: formatDateTimeLocal(startDate),
    lateAt: formatDateTimeLocal(closeDate),
    closeAt: formatDateTimeLocal(closeDate),
    closedAt: null,
    absentProcessed: false,
  };
  attendanceEvents.value.unshift(ev);
  currentAttendanceEventId.value = ev.id;
  scanForm.eventTitle = ev.title;
  notify(`Event started: ${typedName}`);
  return ev;
}

function selectAttendanceEvent(id) {
  const ev = attendanceEvents.value.find((e) => e.id === id);
  if (!ev) return;
  currentAttendanceEventId.value = ev.id;
  scanForm.eventTitle = ev.title;
  scanForm.sessionType = ev.sessionType || 'Log In';
  scanForm.absentFine = Number(ev.absentFine || 0);
  scanForm.openTime = eventTimeValue(ev.openAt) || scanForm.openTime;
  scanForm.closeTime = eventTimeValue(ev.closeAt) || scanForm.closeTime;
  notify(`Event selected: ${ev.title}`);
}

async function closeCurrentAttendanceEvent() {
  const ev = activeAttendanceEvent.value;
  if (!ev) {
    notify('No attendance event is selected');
    return;
  }

  const result = applyAbsentFinesForEvent(ev);
  ev.closedAt = new Date().toISOString();
  ev.absentProcessed = true;
  currentAttendanceEventId.value = null;
  scanForm.eventTitle = '';
  searchTerm.value = '';
  activeView.value = 'fines';
  const attendanceEmailResult = await sendAttendanceConfirmationEmails(ev);
  const emailResult = await sendAbsentFineEmails(ev, result.emailFines);
  const emailText = emailResult.total > 0
    ? ` ${emailResult.sent}/${emailResult.total} email(s) sent now, ${emailResult.queued} queued.`
    : emailResult.noEmail > 0
      ? ` ${emailResult.noEmail} absent student(s) have no email address.`
    : ' No fine emails sent because absent students have no email address.';
  const attendanceEmailText = attendanceEmailResult.total > 0
    ? ` Attendance proof: ${attendanceEmailResult.sent}/${attendanceEmailResult.total} sent, ${attendanceEmailResult.queued} queued.`
    : attendanceEmailResult.noEmail > 0
      ? ` ${attendanceEmailResult.noEmail} present student(s) have no email address.`
      : '';
  notify(`Event closed. ${result.absentCount} absent, ${result.fineCount} fine(s) added.${attendanceEmailText}${emailText}`);
}

function applyAbsentFinesForEvent(event) {
  const eventTitle = event.title;
  const absentFine = Number(event.absentFine || 0);
  const category = `Absent - ${eventTitle}${event.sessionType ? ` (${event.sessionType})` : ''}`;
  const closedAt = new Date().toISOString();
  const addedFines = [];
  const emailFines = [];
  let absentCount = 0;
  let fineCount = 0;

  for (const student of students.value) {
    const existingRecord = attendanceRecords.value.find(
      (record) => record.event === eventTitle && record.studentId === student.id,
    );

    if (existingRecord && existingRecord.status !== 'Absent') {
      continue;
    }

    if (!existingRecord) {
      addAttendanceLocal(eventTitle, student.id, 'Absent', closedAt);
      absentCount += 1;
    }

    const existingFine = fines.value.find(
      (fine) => fine.studentId === student.id && fine.category === category,
    );

    if (existingFine) {
      if (!existingFine.emailNoticeSentAt) {
        emailFines.push(existingFine);
      }
      continue;
    }

    const fine = {
      id: nextId(fines.value),
      studentId: student.id,
      category,
      amount: absentFine,
      status: 'Unpaid',
      emailNoticeSentAt: null,
    };

    fines.value.unshift(fine);
    addedFines.push(fine);
    emailFines.push(fine);
    fineCount += 1;
  }

  if (absentCount > 0) {
    logActivity('Attendance', `${eventTitle} closed`, `${absentCount} absent student(s), ${fineCount} fine(s) added`);
  }

  return { absentCount, fineCount, addedFines, emailFines };
}

async function sendAbsentFineEmails(event, addedFines) {
  const fineTargets = addedFines.map((fine) => ({
    fine,
    student: students.value.find((item) => item.id === fine.studentId),
  }));
  const emailTargets = fineTargets.filter((item) => String(item.student?.email || '').trim());

  let sent = 0;
  let queued = 0;
  const noEmail = fineTargets.length - emailTargets.length;

  for (const item of emailTargets) {
    const totalUnpaid = fines.value
      .filter((fine) => fine.studentId === item.student.id && fine.status !== 'Paid')
      .reduce((sum, fine) => sum + Number(fine.amount || 0), 0);
    const payload = {
      toEmail: item.student.email,
      studentName: item.student.name,
      eventTitle: event.title,
      sessionType: event.sessionType || 'Attendance',
      newFineAmount: money(item.fine.amount || 0),
      totalUnpaidFines: money(totalUnpaid),
      customMessage: '',
      date: new Date().toLocaleString(),
    };

    const sendResult = await sendFineNoticePayload(payload);
    if (sendResult.ok) {
      recordSentEmail(`fine-notice-${item.fine.id}`, item.fine, payload);
      sent += 1;
    } else {
      queueFineNoticeEmail(`fine-notice-${item.fine.id}`, item.fine.id, payload, sendResult.error);
      queued += 1;
    }
  }

  return { sent, queued, total: emailTargets.length, noEmail };
}

async function sendComposedEmails() {
  const message = String(emailComposer.message || '').trim();
  const subject = String(emailComposer.subject || 'Payment reminder').trim() || 'Payment reminder';

  if (!message) {
    notify('Enter a message before sending');
    return;
  }

  const targets = emailComposer.mode === 'specific'
    ? studentsWithEmail.value.filter((student) => student.id === emailComposer.studentId)
    : studentsWithEmail.value;

  if (!targets.length) {
    notify(emailComposer.mode === 'specific' ? 'Select a student with email' : 'No students with email found');
    return;
  }

  let sent = 0;
  let queued = 0;
  const createdAt = Date.now();

  for (const student of targets) {
    const payload = {
      toEmail: student.email,
      studentName: student.name,
      subject,
      message,
      date: new Date().toLocaleString(),
    };
    const id = `general-message-${createdAt}-${student.id}`;
    const sendResult = await sendGeneralMessagePayload(payload);

    if (sendResult.ok) {
      recordSentEmail(id, null, payload, 'general-message');
      sent += 1;
    } else {
      queueEmail(id, 'general-message', null, payload, sendResult.error);
      queued += 1;
    }
  }

  emailComposer.message = '';
  notify(`${sent}/${targets.length} email(s) sent now, ${queued} queued`);
}

async function sendFineNoticePayload(payload) {
  try {
    const response = await apiFetch('/api/email/fine-notice', {
      method: 'POST',
      body: JSON.stringify(payload),
    });

    if (response.ok) {
      return { ok: true, error: '' };
    }

    return { ok: false, error: await responseMessage(response) };
  } catch (error) {
    return { ok: false, error: error instanceof Error ? error.message : 'Unable to reach backend email service' };
  }
}

async function sendGeneralMessagePayload(payload) {
  try {
    const response = await apiFetch('/api/email/message', {
      method: 'POST',
      body: JSON.stringify(payload),
    });

    if (response.ok) {
      return { ok: true, error: '' };
    }

    return { ok: false, error: await responseMessage(response) };
  } catch (error) {
    return { ok: false, error: error instanceof Error ? error.message : 'Unable to reach backend email service' };
  }
}

async function sendAttendanceConfirmationEmail(student, eventTitle, status, recordedAt, method) {
  if (!String(student?.email || '').trim()) {
    return { sent: false, queued: false, noEmail: true };
  }

  if (String(status || '').toLowerCase() === 'absent') {
    return { sent: false, queued: false, noEmail: false };
  }

  const payload = {
    toEmail: student.email,
    studentName: student.name,
    subject: `Attendance recorded - ${eventTitle}`,
    message: `Thank you for attending ${eventTitle}.\n\nYour attendance has been recorded for this event.\n\nStatus: ${status}\nRecorded at: ${formatRecordTime(recordedAt) || new Date().toLocaleString()}\nMethod: ${method}\n\nPlease keep this email as proof of your attendance for future reference.`,
    date: new Date().toLocaleString(),
  };
  const id = `attendance-confirmation-${eventTitle}-${student.id}`.replace(/\s+/g, '-').toLowerCase();
  const sendResult = await sendGeneralMessagePayload(payload);

  if (sendResult.ok) {
    recordSentEmail(id, null, payload, 'attendance-confirmation');
    return { sent: true, queued: false, noEmail: false };
  }

  queueEmail(id, 'attendance-confirmation', null, payload, sendResult.error);
  return { sent: false, queued: true, noEmail: false };
}

async function sendAttendanceConfirmationEmails(event) {
  const records = attendanceRecords.value.filter(
    (record) => record.event === event.title && String(record.status || '').toLowerCase() !== 'absent',
  );
  const targets = records
    .map((record) => ({
      record,
      student: students.value.find((item) => item.id === record.studentId),
    }))
    .filter((item) => item.student);

  let sent = 0;
  let queued = 0;
  const noEmail = targets.filter((item) => !String(item.student.email || '').trim()).length;

  for (const item of targets) {
    const result = await sendAttendanceConfirmationEmail(
      item.student,
      event.title,
      item.record.status,
      item.record.recordedAt,
      'Event close',
    );

    if (result.sent) {
      sent += 1;
    } else if (result.queued) {
      queued += 1;
    }
  }

  return { sent, queued, total: targets.length - noEmail, noEmail };
}

function queueFineNoticeEmail(id, fineId, payload, lastError = '') {
  queueEmail(id, 'fine-notice', fineId, payload, lastError);
}

function queueEmail(id, type, fineId, payload, lastError = '') {
  const existing = pendingEmails.value.find((email) => email.id === id);
  if (existing) {
    existing.lastError = lastError || existing.lastError || '';
    existing.lastTriedAt = new Date().toISOString();
    existing.attempts = Number(existing.attempts || 0) + 1;
    return;
  }

  pendingEmails.value.push({
    id,
    type,
    fineId,
    payload,
    createdAt: new Date().toISOString(),
    lastTriedAt: null,
    lastError,
    attempts: lastError ? 1 : 0,
  });
}

async function processPendingEmails(options = {}) {
  if (!pendingEmails.value.length) {
    return;
  }

  let sent = 0;

  for (const email of [...pendingEmails.value]) {
    email.lastTriedAt = new Date().toISOString();
    email.attempts = Number(email.attempts || 0) + 1;
    const sendResult = email.type === 'fine-notice'
      ? await sendFineNoticePayload(email.payload)
      : await sendGeneralMessagePayload(email.payload);
    if (!sendResult.ok) {
      email.lastError = sendResult.error;
      continue;
    }

    pendingEmails.value = pendingEmails.value.filter((item) => item.id !== email.id);
    const fine = fines.value.find((item) => item.id === email.fineId);
    recordSentEmail(email.id, fine, email.payload, email.type);
    sent += 1;
  }

  if (sent > 0 && !options.silent) {
    notify(`${sent} queued email(s) sent`);
  }
}

function recordSentEmail(id, fine, payload, type = 'fine-notice') {
  const sentAt = new Date().toISOString();
  if (fine) {
    fine.emailNoticeSentAt = sentAt;
  }

  const existingIndex = sentEmails.value.findIndex((email) => email.id === id);
  const entry = {
    id,
    type,
    fineId: fine?.id || null,
    payload,
    sentAt,
  };

  if (existingIndex >= 0) {
    sentEmails.value.splice(existingIndex, 1, entry);
  } else {
    sentEmails.value.unshift(entry);
  }
}

async function startQrScanner() {
  // Ensure an attendance event exists before starting continuous scanning
  if (!currentAttendanceEventId.value) {
    // create a new event using the scanForm eventTitle (or default)
    const event = createAttendanceEvent(scanForm.eventTitle);
    if (!event) {
      return;
    }
  }
  if (!navigator.mediaDevices?.getUserMedia) {
    scannerMessage.value = 'This browser cannot open the camera here. Use Take QR Photo or manual input.';
    return;
  }

  try {
    scannerStream = await navigator.mediaDevices.getUserMedia({
      video: { facingMode: { ideal: 'environment' } },
      audio: false,
    });
    scannerVideo.value.srcObject = scannerStream;
    await scannerVideo.value.play();
    scannerActive.value = true;
    scannerMessage.value = 'Camera is scanning. Point it at a student ID QR code.';

    const canvas = document.createElement('canvas');
    const context = canvas.getContext('2d', { willReadFrequently: true });

    scannerTimer = window.setInterval(async () => {
      if (!scannerVideo.value || scannerVideo.value.readyState < 2) {
        return;
      }

      canvas.width = scannerVideo.value.videoWidth;
      canvas.height = scannerVideo.value.videoHeight;

      if (!canvas.width || !canvas.height) {
        return;
      }

      context.drawImage(scannerVideo.value, 0, 0, canvas.width, canvas.height);
      const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
      const decoded = jsQR(imageData.data, imageData.width, imageData.height);

      if (decoded?.data) {
        await recordQrScan(decoded.data);
      }
    }, 250);
  } catch (error) {
    scannerMessage.value = error instanceof Error ? error.message : 'Unable to open camera.';
  }
}

function stopQrScanner() {
  if (scannerTimer) {
    window.clearInterval(scannerTimer);
    scannerTimer = null;
  }

  if (scannerStream) {
    scannerStream.getTracks().forEach((track) => track.stop());
    scannerStream = null;
  }

  scannerActive.value = false;
  if (scannerVideo.value) {
    scannerVideo.value.srcObject = null;
  }
}

async function recordQrScan(rawValue, options = {}) {
  const studentNo = parseQrPayload(rawValue);
  const now = Date.now();

  if (!studentNo) {
    scannerMessage.value = 'Scan or type a student ID first.';
    return;
  }

  if (studentNo === lastScan.value && now - lastScan.time < scanCooldownMs) {
    return;
  }

  lastScan = { value: studentNo, time: now };
  let student = students.value.find((item) => item.studentNo === studentNo);
  let rfid = null;

  if (!student) {
    const normalizedValue = normalizeRfid(studentNo);
    student = students.value.find((item) => normalizeRfid(item.rfidUid) === normalizedValue);
    if (student) {
      rfid = normalizedValue;
    }
  }

  if (!student) {
    scannerMessage.value = `Student ID/RFID ${studentNo || '(blank)'} was not found.`;
    notify('Student not found');
    return;
  }

  const eventTitle = (attendanceEvents.value.find(e => e.id === currentAttendanceEventId.value)?.title) || scanForm.eventTitle || attendanceForm.event || 'Attendance Scan';
  const existingRecord = findAttendanceRecord(eventTitle, student.id);
  if (existingRecord) {
    manualQr.value = '';
    clearQuickScan();
    showAlreadyRecorded(student, eventTitle, existingRecord, 'QR / Student ID');
    return;
  }

  let savedRemotely = false;
  let recordedStatus = scanForm.status || attendanceForm.status || 'Present';
  let alreadyRecorded = false;

  try {
    const response = await apiFetch('/api/attendance/scan', {
      method: 'POST',
      body: JSON.stringify({
          studentNo: rfid ? null : studentNo,
          rfidUid: rfid,
          eventTitle,
          status: recordedStatus,
          ...attendanceWindowPayload(),
          location: 'QR scanner',
          remarks: rfid ? 'Recorded from RFID value via ID/QR field.' : 'Recorded from student ID QR.',
        }),
    });

    if (!response.ok) {
      throw new Error(await responseMessage(response));
    }

    const result = await response.json();
    recordedStatus = result.status;
    const localResult = addAttendanceLocal(result.event || eventTitle, student.id, recordedStatus, result.recordedAt);
    alreadyRecorded = Boolean(result.isDuplicate || !localResult?.created);
    if (alreadyRecorded) {
      const duplicateEvent = result.event || eventTitle;
      const duplicateRecord = localResult?.record || findAttendanceRecord(duplicateEvent, student.id);
      manualQr.value = '';
      clearQuickScan();
      showAlreadyRecorded(student, duplicateEvent, duplicateRecord, 'QR / Student ID');
      return;
    }
    showLateFineResult(result);
    showScanPop(student, recordedStatus, result.event || eventTitle, 'QR / Student ID');
    savedRemotely = true;
  } catch (error) {
    const localResult = addAttendanceLocal(eventTitle, student.id, recordedStatus);
    alreadyRecorded = !localResult?.created;
    if (alreadyRecorded) {
      manualQr.value = '';
      clearQuickScan();
      showAlreadyRecorded(student, eventTitle, localResult?.record || findAttendanceRecord(eventTitle, student.id), 'QR / Student ID');
      return;
    }
    showScanPop(student, recordedStatus, eventTitle, 'QR / Student ID');
    scannerMessage.value = `${student.name} saved locally. Backend scan failed: ${error instanceof Error ? error.message : 'Unable to reach backend'}`;
  }

  manualQr.value = '';
  clearQuickScan();
  if (alreadyRecorded) {
    showAlreadyRecorded(student, eventTitle, findAttendanceRecord(eventTitle, student.id), 'QR / Student ID');
    return;
  }

  if (!scannerMessage.value.includes('Fine added')) {
    scannerMessage.value = savedRemotely
      ? `${student.name} recorded for ${eventTitle}.`
      : `${student.name} saved locally for ${eventTitle}.`;
  }

  if (!options.silent) {
    notify(savedRemotely ? `${student.name} attendance recorded` : `${student.name} attendance saved locally`);
  }
}

async function recordQrPhoto(event) {
  const file = event.target.files?.[0];
  event.target.value = '';

  if (!file) {
    return;
  }

  try {
    const bitmap = await createImageBitmap(file);
    const canvas = document.createElement('canvas');
    canvas.width = bitmap.width;
    canvas.height = bitmap.height;

    const context = canvas.getContext('2d', { willReadFrequently: true });
    context.drawImage(bitmap, 0, 0);

    const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
    const decoded = jsQR(imageData.data, imageData.width, imageData.height);

    if (!decoded?.data) {
      scannerMessage.value = 'No QR code found in that photo. Try a clearer, closer shot.';
      notify('No QR found');
      return;
    }

    await recordQrScan(decoded.data);
  } catch (error) {
    scannerMessage.value = error instanceof Error ? error.message : 'Unable to read that QR photo.';
  }
}

async function recordRfidScan(rawValue) {
  const rfidUid = normalizeRfid(rawValue);
  const now = Date.now();

  if (!rfidUid) {
    scannerMessage.value = 'Tap or type an RFID UID first.';
    return;
  }

  if (rfidUid === lastScan.value && now - lastScan.time < scanCooldownMs) {
    return;
  }

  lastScan = { value: rfidUid, time: now };

  const student = students.value.find((item) => normalizeRfid(item.rfidUid) === rfidUid);
  if (!student) {
    scannerMessage.value = `RFID ${rfidUid || '(blank)'} is not mapped to a student.`;
    notify('RFID not mapped');
    return;
  }

  const eventTitle = (attendanceEvents.value.find(e => e.id === currentAttendanceEventId.value)?.title) || scanForm.eventTitle || attendanceForm.event || 'Attendance Scan';
  const existingRecord = findAttendanceRecord(eventTitle, student.id);
  if (existingRecord) {
    manualRfid.value = '';
    clearQuickScan();
    showAlreadyRecorded(student, eventTitle, existingRecord, 'RFID');
    return;
  }

  let savedRemotely = false;
  let recordedStatus = scanForm.status || attendanceForm.status || 'Present';
  let alreadyRecorded = false;

  try {
    const response = await apiFetch('/api/attendance/scan', {
      method: 'POST',
      body: JSON.stringify({
          studentNo: null,
          rfidUid,
          eventTitle,
          status: recordedStatus,
          ...attendanceWindowPayload(),
          location: 'RFID reader',
          remarks: 'Recorded from RFID card.',
        }),
    });

    if (!response.ok) {
      throw new Error(await responseMessage(response));
    }

    const result = await response.json();
    recordedStatus = result.status;
    const localResult = addAttendanceLocal(result.event || eventTitle, student.id, recordedStatus, result.recordedAt);
    alreadyRecorded = Boolean(result.isDuplicate || !localResult?.created);
    if (alreadyRecorded) {
      const duplicateEvent = result.event || eventTitle;
      const duplicateRecord = localResult?.record || findAttendanceRecord(duplicateEvent, student.id);
      manualRfid.value = '';
      clearQuickScan();
      showAlreadyRecorded(student, duplicateEvent, duplicateRecord, 'RFID');
      return;
    }
    showLateFineResult(result);
    showScanPop(student, recordedStatus, result.event || eventTitle, 'RFID');
    savedRemotely = true;
  } catch (error) {
    const localResult = addAttendanceLocal(eventTitle, student.id, recordedStatus);
    alreadyRecorded = !localResult?.created;
    if (alreadyRecorded) {
      manualRfid.value = '';
      clearQuickScan();
      showAlreadyRecorded(student, eventTitle, localResult?.record || findAttendanceRecord(eventTitle, student.id), 'RFID');
      return;
    }
    showScanPop(student, recordedStatus, eventTitle, 'RFID');
    scannerMessage.value = `${student.name} saved locally. Backend RFID failed: ${error instanceof Error ? error.message : 'Unable to reach backend'}`;
  }

  manualRfid.value = '';
  clearQuickScan();
  if (alreadyRecorded) {
    showAlreadyRecorded(student, eventTitle, findAttendanceRecord(eventTitle, student.id), 'RFID');
    return;
  }

  if (!scannerMessage.value.includes('Fine added')) {
    scannerMessage.value = savedRemotely
      ? `${student.name} recorded by RFID for ${eventTitle}.`
      : `${student.name} saved locally by RFID for ${eventTitle}.`;
  }
  notify(savedRemotely ? `${student.name} RFID attendance recorded` : `${student.name} RFID attendance saved locally`);
}

async function recordAnyScan(rawValue) {
  const value = String(rawValue || '').trim();
  if (!value) {
    scannerMessage.value = 'Scan an RFID, QR, or student ID first.';
    focusQuickScan();
    return;
  }

  const studentNo = parseQrPayload(value);
  const normalizedValue = normalizeRfid(value);
  const studentByNumber = students.value.find((student) => student.studentNo === studentNo);
  const studentByRfid = students.value.find((student) => normalizeRfid(student.rfidUid) === normalizedValue);

  if (value.toUpperCase().startsWith('KIER:') || studentByNumber) {
    await recordQrScan(studentNo);
  } else if (studentByRfid) {
    await recordRfidScan(value);
  } else {
    scannerMessage.value = `${value} is not mapped to a student ID or RFID.`;
    notify('Scan not mapped');
  }

  focusQuickScan();
}

function clearQuickScan() {
  quickScanValue.value = '';
  focusQuickScan();
}

function focusQuickScan() {
  window.setTimeout(() => quickScanInput.value?.focus(), 0);
}

function showLateFineResult(result) {
  if (Number(result?.lateFineAmount) > 0) {
    scannerMessage.value = `Late by ${result.minutesLate} minute(s). Fine added: ${money(result.lateFineAmount)}.`;
  }
}

function showScanPop(student, status, event, method) {
  if (scanPopTimer) {
    window.clearTimeout(scanPopTimer);
  }

  Object.assign(scanPop, {
    visible: true,
    name: student.name,
    studentNo: student.studentNo,
    method,
    status,
    event,
    time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' }),
  });

  scanPopTimer = window.setTimeout(() => {
    scanPop.visible = false;
  }, 3000);
}

async function responseMessage(response) {
  const text = await response.text();

  try {
    const result = JSON.parse(text);
    const detail = [result.message, result.error, result.detail].filter(Boolean).join(' ');
    return detail || text;
  } catch {
    return text || `Request failed with ${response.status}`;
  }
}

function removeAttendance(recordId) {
  attendanceRecords.value = attendanceRecords.value.filter((record) => record.id !== recordId);
  logActivity('Attendance', 'Attendance removed', 'Attendance record deleted');
  notify('Attendance deleted');
}

function addDisbursement() {
  if (editingDisbursementId.value) {
    const existing = disbursements.value.find((d) => d.id === editingDisbursementId.value);
    if (existing) {
      existing.description = disbursementForm.description;
      existing.usedBy = disbursementForm.usedBy;
      existing.amount = Number(disbursementForm.amount || 0);
      existing.month = currentMonth();
    }
  } else {
    disbursements.value.unshift({
      id: nextId(disbursements.value),
      description: disbursementForm.description,
      usedBy: disbursementForm.usedBy,
      amount: Number(disbursementForm.amount || 0),
      month: currentMonth(),
    });
  }
  logActivity('Expense', `${disbursementForm.description} recorded`, `${disbursementForm.usedBy || 'Not specified'} - ${money(disbursementForm.amount)}`);
  disbursementForm.description = 'Department expense';
  disbursementForm.usedBy = '';
  disbursementForm.amount = 100;
  editingDisbursementId.value = null;
  notify('Expense saved');
}

function removeDisbursement(disbursementId) {
  disbursements.value = disbursements.value.filter((item) => item.id !== disbursementId);
  logActivity('Expense', 'Expense removed', 'Disbursement record deleted');
  notify('Expense deleted');
}

function editDisbursement(disbursement) {
  editingDisbursementId.value = disbursement.id;
  disbursementForm.description = disbursement.description;
  disbursementForm.usedBy = disbursement.usedBy || '';
  disbursementForm.amount = disbursement.amount;
  setView('admin');
}

function primaryAction() {
  if (activeView.value === 'dashboard') {
    setView('collections');
  } else if (activeView.value === 'students') {
    resetStudentForm();
    notify('Student form is ready');
  } else if (activeView.value === 'admin') {
    setView('collections');
  } else if (activeView.value === 'emails') {
    processPendingEmails();
  } else if (activeView.value === 'transactions') {
    setView('reports');
  } else if (activeView.value === 'reports') {
    windowPrint();
  }
}

function exportCsv() {
  const rows = [
    ['Type', 'Reference', 'Student', 'Category', 'Amount', 'Status', 'Time In'],
    ...collections.value.map((item) => [
      item.status === 'Paid' ? 'Receipt' : 'Bill',
      item.receipt,
      studentName(item.studentId),
      item.category,
      item.amount,
      item.status,
    ]),
    ...fines.value.map((item) => ['Fine', `FINE-${item.id}`, studentName(item.studentId), item.category, item.amount, item.status]),
    ...disbursements.value.map((item) => [
      'Fund / Expense',
      `EXP-${item.id}`,
      item.usedBy || 'Not specified',
      item.description,
      item.amount,
      'Withdrawn',
      item.month || '',
    ]),
    ...attendanceRecords.value.map((item) => [
      'Attendance',
      item.event,
      studentName(item.studentId),
      item.status,
      '',
      'Recorded',
      formatRecordTime(item.recordedAt),
    ]),
  ];
  const csv = rows.map((row) => row.map((cell) => `"${String(cell).replaceAll('"', '""')}"`).join(',')).join('\n');
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = 'kier-records-export.csv';
  link.click();
  URL.revokeObjectURL(url);
  notify('CSV exported');
}

function windowPrint() {
  window.print();
}

function saveState() {
  localStorage.setItem(
    storageKey,
    JSON.stringify({
      students: students.value,
      collections: collections.value,
      fines: fines.value,
      attendanceEvents: attendanceEvents.value,
      currentAttendanceEventId: currentAttendanceEventId.value,
      attendanceRecords: attendanceRecords.value,
      disbursements: disbursements.value,
      activity: activity.value,
      pendingEmails: pendingEmails.value,
      sentEmails: sentEmails.value,
    }),
  );
}

function loadState() {
  const stored = localStorage.getItem(storageKey);
  if (!stored) {
    students.value = [];
    collections.value = [];
    fines.value = [];
    attendanceEvents.value = [];
    currentAttendanceEventId.value = null;
    attendanceRecords.value = [];
    disbursements.value = [];
    activity.value = [];
    pendingEmails.value = [];
    sentEmails.value = [];
    return;
  }

  try {
    const parsed = JSON.parse(stored);
    students.value = Array.isArray(parsed.students)
      ? parsed.students.map((student) => normalizeStudentRecord(student))
      : [];
    collections.value = Array.isArray(parsed.collections) ? parsed.collections : [];
    fines.value = Array.isArray(parsed.fines) ? parsed.fines : [];
    attendanceEvents.value = Array.isArray(parsed.attendanceEvents) ? parsed.attendanceEvents : [];
    currentAttendanceEventId.value = parsed.currentAttendanceEventId || null;
    attendanceRecords.value = Array.isArray(parsed.attendanceRecords) ? parsed.attendanceRecords : [];
    disbursements.value = Array.isArray(parsed.disbursements) ? parsed.disbursements : [];
    activity.value = Array.isArray(parsed.activity) ? parsed.activity : [];
    pendingEmails.value = Array.isArray(parsed.pendingEmails) ? parsed.pendingEmails : [];
    sentEmails.value = Array.isArray(parsed.sentEmails) ? parsed.sentEmails : [];
    if (currentAttendanceEventId.value) {
      const activeEvent = attendanceEvents.value.find((event) => event.id === currentAttendanceEventId.value);
      if (activeEvent) {
        scanForm.eventTitle = activeEvent.title;
      }
    }
  } catch {
    resetState();
  }
}

function resetState() {
  students.value = [];
  collections.value = [];
  fines.value = [];
  attendanceRecords.value = [];
  disbursements.value = [];
  activity.value = [];
  pendingEmails.value = [];
  sentEmails.value = [];
  selectedStudentId.value = null;
  resetStudentForm();
  localStorage.removeItem(storageKey);
  notify('State reset');
}

function confirmResetState() {
  if (confirm('This will delete all local app records and start fresh. Proceed?')) {
    resetState();
  }
}
</script>

<style>
* {
  box-sizing: border-box;
}

body {
  margin: 0;
  background: #eef2f3;
  color: #142027;
}

button,
input,
select,
textarea {
  font: inherit;
}

button {
  cursor: pointer;
}

#app {
  min-height: 100vh;
  font-family: Inter, Segoe UI, Arial, sans-serif;
}

.workspace {
  display: grid;
  grid-template-columns: 268px minmax(0, 1fr);
  min-height: 100vh;
}

.sidebar {
  position: sticky;
  top: 0;
  display: flex;
  flex-direction: column;
  gap: 22px;
  height: 100vh;
  padding: 22px;
  background: #142027;
  color: #ffffff;
}

.brand,
.api-card {
  display: flex;
  gap: 12px;
  align-items: center;
}

.brand-mark {
  display: grid;
  width: 44px;
  height: 44px;
  place-items: center;
  border-radius: 8px;
  background: #f59e0b;
  color: #142027;
  font-weight: 900;
}

.brand small,
.api-card small,
.sidebar-summary small {
  display: block;
  margin-top: 4px;
  color: #a9b7be;
}

.nav-list {
  display: grid;
  gap: 8px;
}

.nav-list button {
  display: flex;
  gap: 10px;
  align-items: center;
  min-height: 42px;
  padding: 0 12px;
  border: 0;
  border-radius: 8px;
  background: transparent;
  color: #d8e0e4;
  text-align: left;
  cursor: pointer;
  transition: background 180ms ease, color 180ms ease;
}

.nav-list button span {
  display: grid;
  width: 26px;
  height: 26px;
  place-items: center;
  border-radius: 6px;
  background: #24343d;
  font-size: 0.72rem;
  font-weight: 900;
}

.nav-list button.active,
.nav-list button:hover {
  background: #23343d;
  color: #ffffff;
}

.sidebar-summary,
.api-card {
  padding: 15px;
  border: 1px solid #31444f;
  border-radius: 8px;
  background: #1a2a32;
}

.sidebar-summary span {
  color: #a9b7be;
  font-size: 0.78rem;
  font-weight: 800;
  text-transform: uppercase;
}

.sidebar-summary strong {
  display: block;
  margin-top: 8px;
  font-size: 1.4rem;
}

.api-card {
  margin-top: auto;
}

.status-dot {
  width: 12px;
  height: 12px;
  flex: 0 0 12px;
  border-radius: 999px;
  background: #f59e0b;
}

.status-dot.online {
  background: #16a34a;
}

.content {
  position: relative;
  min-width: 0;
  padding: 28px;
}

.topbar {
  display: flex;
  gap: 24px;
  align-items: flex-end;
  justify-content: space-between;
  margin-bottom: 18px;
}

.eyebrow {
  margin: 0 0 6px;
  color: #60717a;
  font-size: 0.78rem;
  font-weight: 800;
  text-transform: uppercase;
}

h1,
h2,
p {
  margin-top: 0;
}

h1 {
  margin-bottom: 0;
  font-size: 2rem;
  line-height: 1.15;
  letter-spacing: 0;
}

h2 {
  margin-bottom: 0;
  font-size: 1rem;
}

.toolbar,
.button-row {
  display: flex;
  gap: 10px;
  align-items: end;
}

.panel.event-creator-panel .panel-body {
  display: grid;
  gap: 12px;
}

.panel.event-creator-panel .panel-body label {
  display: grid;
  gap: 6px;
}

.panel.event-creator-panel .event-details-grid {
  display: grid;
  grid-template-columns: minmax(180px, 1fr) minmax(180px, 1fr);
  gap: 10px;
  align-items: end;
}

.panel.event-creator-panel .button-row {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  align-items: center;
  margin-top: 0;
}

.qr-scanner-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 20px;
}

@media (min-width: 1080px) {
  .qr-scanner-grid {
    grid-template-columns: minmax(320px, 420px) minmax(0, 1fr);
    align-items: start;
  }
}

.scanner-panel {
  display: grid;
  gap: 0;
}

.scanner-body {
  display: grid;
  gap: 20px;
  padding: 20px;
}

.scanner-controls {
  display: grid;
  gap: 18px;
}

.fast-scan-strip {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 12px;
  align-items: end;
}

.fast-scan-strip label {
  display: grid;
  gap: 6px;
}

.scan-settings summary {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px;
  border: 1px solid #dbe5e8;
  border-radius: 12px;
  background: #fbfdfb;
  cursor: pointer;
}

.scan-settings summary strong {
  color: #142027;
}

.scan-settings .event-controls,
.compact-grid {
  display: grid;
  gap: 12px;
}

.scan-settings .current-event-display {
  display: grid;
  gap: 6px;
  padding: 14px 0 0;
  color: #142027;
}

.compact-grid {
  grid-template-columns: repeat(2, minmax(140px, 1fr));
}

.scan-fallbacks {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.scan-fallbacks label {
  display: grid;
  gap: 6px;
}

.photo-input input {
  padding: 10px 12px;
}

.camera-slot {
  min-height: 260px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 18px;
  border: 1px dashed #dbe5e8;
  border-radius: 16px;
  background: #f7fbfa;
}

.scanner-video {
  width: 100%;
  max-height: 260px;
  border-radius: 14px;
  object-fit: cover;
}

.scanner-message {
  color: #60717a;
  font-size: 0.95rem;
  line-height: 1.5;
}

.scan-pop-card {
  display: grid;
  gap: 8px;
  padding: 16px;
  border-radius: 14px;
  border: 1px solid #cce7df;
  background: #eff9f4;
}

.scan-pop-card.present {
  border-color: #16a34a;
  background: #ecfdf5;
}

.scan-pop-card.late {
  border-color: #f59e0b;
  background: #fffbeb;
}

.scan-pop-card.absent,
.scan-pop-card.excused {
  border-color: #7c3aed;
  background: #f5f3ff;
}

.scan-pop-card span,
.scan-pop-card small,
.scan-pop-card em {
  color: #475569;
}

.manual-panel summary {
  padding: 16px 20px;
  border-bottom: 1px solid #e2e8eb;
  background: #fbfbfb;
  border-radius: 12px;
}

.manual-form {
  display: grid;
  gap: 14px;
  padding: 20px;
}

.manual-form label {
  display: grid;
  gap: 6px;
}

.manual-form button {
  justify-self: start;
}

.panel.event-creator-panel .panel-body {
  display: grid;
  gap: 12px;
}

.admin-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(260px, 1fr));
  gap: 18px;
  padding: 20px;
}

.admin-card {
  display: grid;
  gap: 14px;
  align-content: start;
  border: 1px solid #e2e8eb;
  border-radius: 8px;
  padding: 18px;
  background: #ffffff;
}

.admin-card h3,
.admin-card p {
  margin: 0;
}

.admin-card p {
  color: #60717a;
  font-size: 0.92rem;
}

.admin-card label {
  display: grid;
  gap: 6px;
  color: #60717a;
  font-size: 0.82rem;
  font-weight: 800;
}

.admin-card input {
  width: 100%;
}

.admin-card .button-row {
  justify-content: flex-end;
  margin-top: 4px;
}

.search {
  display: grid;
  gap: 6px;
  color: #60717a;
  font-size: 0.78rem;
  font-weight: 700;
}

input,
select,
textarea {
  min-height: 40px;
  min-width: 190px;
  border: 1px solid #ccd6db;
  border-radius: 8px;
  padding: 0 12px;
  background: #ffffff;
  color: #142027;
}

textarea {
  min-height: 88px;
  padding-top: 10px;
  resize: vertical;
}

.auth-overlay {
  position: fixed;
  inset: 0;
  display: grid;
  place-items: center;
  background: rgba(20, 32, 39, 0.55);
  padding: 24px;
  z-index: 40;
}

.auth-card {
  width: min(460px, 100%);
  display: grid;
  gap: 18px;
  padding: 28px 26px;
  border-radius: 24px;
  background: linear-gradient(180deg, #ffffff 0%, #f7fbfc 100%);
  box-shadow: 0 24px 52px rgba(11, 25, 31, 0.18);
  border: 1px solid rgba(15, 118, 110, 0.12);
}

.auth-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
}

.auth-header h2 {
  margin: 0;
  font-size: 1.6rem;
}

.auth-header p {
  margin: 8px 0 0;
  color: #4b626d;
  line-height: 1.55;
}

.auth-label {
  display: inline-flex;
  padding: 5px 12px;
  border-radius: 999px;
  background: #e0f3f1;
  color: #0f766e;
  font-size: 0.78rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.auth-field {
  display: grid;
  gap: 8px;
}

.auth-field label {
  color: #22313d;
  font-weight: 700;
}

.auth-card input {
  border-color: #d7dde1;
  padding: 0 14px;
  font-size: 0.98rem;
}

.auth-card .primary-action {
  width: 100%;
  justify-self: stretch;
  padding: 0 16px;
  min-height: 48px;
  font-size: 1rem;
}

.auth-hint {
  color: #52616b;
  font-size: 0.84rem;
  line-height: 1.5;
  margin: 0;
}

.text-action {
  min-height: auto;
  border: 0;
  padding: 0;
  background: transparent;
  color: #0f766e;
  font-weight: 800;
}

.primary-action,
.secondary-action,
.panel-heading button,
.table-actions button,
.text-action,
.drawer-close {
  min-height: 38px;
  border: 0;
  border-radius: 8px;
  padding: 0 13px;
  font-weight: 800;
}

.primary-action {
  background: #0f766e;
  color: #ffffff;
}

.secondary-action,
.panel-heading button,
.table-actions button,
.drawer-close {
  border: 1px solid #cfd9de;
  background: #ffffff;
  color: #20313a;
}

.text-action,
.row-link {
  min-height: auto;
  border: 0;
  padding: 0;
  background: transparent;
  color: #0f766e;
  font-weight: 800;
}

.toast {
  width: fit-content;
  margin: 0 0 16px auto;
  border-radius: 8px;
  padding: 10px 14px;
  background: #142027;
  color: #ffffff;
  font-weight: 700;
}

.view-stack {
  display: grid;
  gap: 20px;
}

.stat-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 16px;
}

.stat-card,
.panel,
.student-drawer {
  border: 1px solid #d7e0e4;
  border-radius: 8px;
  background: #ffffff;
}

.stat-card {
  display: grid;
  gap: 12px;
  min-height: 142px;
  padding: 20px;
}

.stat-card span,
.stat-card small,
.panel-heading span {
  color: #60717a;
}

.stat-card strong {
  font-size: 1.65rem;
}

.split-grid,
.data-layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 20px;
}

.data-layout {
  grid-template-columns: 320px minmax(0, 1fr);
}

.collections-page {
  display: grid;
  gap: 20px;
}

.collections-forms {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 20px;
}

.compact-form p {
  margin: 0;
  color: #60717a;
  font-size: 0.92rem;
}

.compact-heading {
  padding-bottom: 14px;
}

.qr-scanner-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 12px;
}

.panel {
  min-width: 0;
  overflow: hidden;
}

.panel-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 18px 20px;
  border-bottom: 1px solid #e2e8eb;
}

.panel-heading.flat {
  padding: 0 0 18px;
}

.panel.scanner-panel,
.panel.event-creator-panel,
.panel.manual-panel,
.panel.attendance-list-panel {
  border: 1px solid #d7e0e4;
  border-radius: 16px;
  background: #ffffff;
}

.attendance-list-panel table {
  border-radius: 16px;
  overflow: hidden;
}

.attendance-list-panel th,
.attendance-list-panel td {
  padding: 14px 18px;
}

.attendance-list-panel tbody tr:nth-child(odd) {
  background: #fbfcfd;
}

.attendance-list-panel tbody tr:hover {
  background: #eef7f4;
}

.badge.neutral {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 6px 12px;
  border-radius: 999px;
  background: #eef6f6;
  color: #0f766e;
  font-size: 0.85rem;
  font-weight: 700;
}

.ledger-panel {
  display: grid;
}

.ledger-total {
  display: grid;
  gap: 2px;
  justify-items: end;
}

.ledger-total span {
  color: #60717a;
  font-size: 0.78rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.ledger-list {
  display: grid;
  gap: 12px;
  padding: 16px 20px 20px;
}

.ledger-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 16px;
  border: 1px solid #e2e8eb;
  border-radius: 10px;
  background: #f9fcfb;
}

.ledger-main {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  flex: 1;
}

.ledger-meta {
  display: grid;
  gap: 2px;
}

.ledger-meta strong {
  color: #142027;
}

.ledger-meta span {
  color: #60717a;
  font-size: 0.92rem;
}

.ledger-category {
  color: #0f766e;
  font-weight: 800;
}

.ledger-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

.ledger-amount {
  color: #142027;
  font-weight: 900;
  white-space: nowrap;
}

.absent-fines-browser {
  display: grid;
  grid-template-columns: minmax(180px, 260px) minmax(0, 1fr);
  min-height: 360px;
}

.student-name-list {
  display: grid;
  align-content: start;
  gap: 8px;
  padding: 16px;
  border-right: 1px solid #e2e8eb;
  background: #f8fbfb;
}

.student-name-list button {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  min-height: 42px;
  padding: 10px 12px;
  border: 1px solid #dbe5e8;
  border-radius: 8px;
  background: #ffffff;
  color: #142027;
  font-weight: 800;
}

.student-name-list button.active,
.student-name-list button:hover {
  border-color: #0f766e;
  background: #edf8f6;
  color: #0f766e;
}

.student-name-list strong {
  min-width: 28px;
  border-radius: 999px;
  background: #e7eef1;
  color: #41535c;
  font-size: 0.82rem;
  text-align: center;
}

.student-name-list button.active strong,
.student-name-list button:hover strong {
  background: #0f766e;
  color: #ffffff;
}

.student-name-list p,
.empty-state {
  margin: 0;
  color: #60717a;
}

.absent-fines-detail {
  display: grid;
  align-content: start;
  gap: 14px;
  padding: 18px 20px 20px;
}

.absent-detail-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.absent-detail-heading h3 {
  margin: 0;
  color: #142027;
}

.absent-detail-heading span {
  color: #60717a;
  font-size: 0.92rem;
}

.absent-total-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
  flex-wrap: wrap;
}

.absent-total-actions > strong {
  color: #9f1239;
  font-size: 1.15rem;
}

.absent-event-list {
  display: grid;
  gap: 10px;
}

.absent-event-list article {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  align-items: center;
  gap: 16px;
  padding: 14px 16px;
  border: 1px solid #e2e8eb;
  border-radius: 8px;
  background: #ffffff;
}

.absent-event-list article div {
  display: grid;
  gap: 2px;
}

.absent-event-list article div:last-child {
  justify-items: end;
}

.absent-event-list span {
  color: #60717a;
  font-size: 0.9rem;
}

.absent-event-list button {
  white-space: nowrap;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th,
td {
  padding: 14px 20px;
  border-bottom: 1px solid #edf1f3;
  text-align: left;
  vertical-align: middle;
}

th {
  color: #60717a;
  font-size: 0.78rem;
  text-transform: uppercase;
}

tbody tr {
  transition: background 0.15s ease;
}

tbody tr:hover {
  background: #f5faf9;
}

.table-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.chart-list {
  display: grid;
  gap: 16px;
  padding: 20px;
}

.chart-row {
  display: grid;
  grid-template-columns: 52px minmax(0, 1fr) 90px;
  gap: 12px;
  align-items: center;
}

.bar-track {
  position: relative;
  height: 18px;
  overflow: hidden;
  border-radius: 999px;
  background: #e6ecef;
}

.bar {
  position: absolute;
  top: 0;
  bottom: 0;
  left: 0;
}

.bar.income {
  background: #0f766e;
}

.bar.expense {
  background: rgba(245, 158, 11, 0.82);
}

.activity-list {
  display: grid;
  gap: 12px;
  margin: 0;
  padding: 20px;
  list-style: none;
}

.activity-list li {
  display: grid;
  grid-template-columns: 82px minmax(0, 1fr);
  gap: 12px;
}

.activity-list li > span {
  align-self: start;
  border-radius: 999px;
  padding: 5px 9px;
  background: #eef7f6;
  color: #0f766e;
  font-size: 0.76rem;
  font-weight: 900;
  text-align: center;
}

.activity-list small {
  display: block;
  margin-top: 3px;
  color: #60717a;
}

.form-panel {
  display: grid;
  gap: 16px;
  align-content: start;
  padding: 20px;
}

.form-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.form-panel label {
  display: grid;
  gap: 7px;
  color: #60717a;
  font-size: 0.82rem;
  font-weight: 800;
}

.form-panel input,
.form-panel select,
.form-panel textarea {
  width: 100%;
  min-width: 0;
}

.autocomplete {
  position: relative;
}

.autocomplete-list {
  position: static;
  margin: 6px 0 0;
  padding: 6px;
  border: 1px solid #d7e0e4;
  border-radius: 8px;
  background: #ffffff;
  box-shadow: 0 10px 24px rgba(20, 32, 39, 0.08);
  list-style: none;
  max-height: 160px;
  overflow-y: auto;
}

.autocomplete-list li {
  border-radius: 6px;
  padding: 8px 10px;
  color: #142027;
  cursor: pointer;
}

.autocomplete-list li:hover {
  background: #f5faf9;
}

.scanner-body {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(180px, 280px);
  gap: 12px;
  padding: 16px;
}

.camera-slot {
  display: grid;
  min-height: 170px;
  overflow: hidden;
  place-items: center;
  border: 1px solid #ccd6db;
  border-radius: 8px;
  background: #142027;
  color: #9fb0b8;
  font-weight: 900;
}

.camera-slot.idle {
  min-height: 112px;
  background: #eef4f6;
  color: #60717a;
}

.scanner-video {
  width: 100%;
  height: 100%;
  min-height: 170px;
  object-fit: cover;
}

.scanner-controls {
  display: grid;
  gap: 10px;
  align-content: start;
}

.scanner-controls label {
  display: grid;
  gap: 5px;
  color: #60717a;
  font-size: 0.82rem;
  font-weight: 800;
}

.scanner-controls input,
.scanner-controls select,
.scanner-controls textarea {
  width: 100%;
}

.fast-scan-strip {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 118px;
  gap: 10px;
  align-items: end;
  border: 1px solid #b9d5ce;
  border-radius: 8px;
  padding: 12px;
  background: #f3fbf8;
}

.fast-scan-strip input {
  min-height: 44px;
  font-size: 1rem;
  font-weight: 800;
}

.compact-grid,
.scan-fallbacks {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.scan-settings {
  display: grid;
  gap: 10px;
  border: 1px solid #d8e3e8;
  border-radius: 8px;
  background: #ffffff;
}

.scan-settings summary,
.manual-panel summary {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  cursor: pointer;
  padding: 12px 14px;
  color: #60717a;
  font-size: 0.82rem;
  font-weight: 900;
  list-style: none;
}

.scan-settings summary::-webkit-details-marker,
.manual-panel summary::-webkit-details-marker {
  display: none;
}

.scan-settings summary::after,
.manual-panel summary::after {
  content: "+";
  display: grid;
  width: 26px;
  height: 26px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 999px;
  background: #eef4f6;
  color: #142027;
}

.scan-settings[open] summary::after,
.manual-panel[open] summary::after {
  content: "-";
}

.scan-settings summary strong {
  min-width: 0;
  overflow: hidden;
  color: #142027;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.scan-settings .compact-grid {
  padding: 0 12px 12px;
}

.compact-grid.three {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.scan-fallbacks {
  grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr) auto minmax(130px, 0.8fr);
  align-items: end;
}

.scan-fallbacks button {
  min-height: 40px;
  padding-inline: 12px;
}

.photo-input input {
  min-height: 40px;
  padding-top: 8px;
  font-size: 0.78rem;
}

.manual-panel {
  padding: 0;
}

.manual-form {
  display: grid;
  grid-template-columns: 1.2fr 1.2fr 0.8fr auto;
  gap: 10px;
  align-items: end;
  padding: 0 16px 16px;
}

.manual-form label {
  display: grid;
  gap: 5px;
  color: #60717a;
  font-size: 0.82rem;
  font-weight: 800;
}

.manual-form input,
.manual-form select,
.manual-form textarea {
  width: 100%;
}

.scanner-message {
  grid-column: 1 / -1;
  margin: 0;
  border-radius: 8px;
  padding: 10px 12px;
  background: #f4f8f9;
  color: #394a53;
}

.scan-pop-card {
  grid-column: 1 / -1;
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 4px 14px;
  align-items: center;
  border: 1px solid #b7dfd3;
  border-left: 6px solid #0f766e;
  border-radius: 8px;
  padding: 16px 18px;
  background: #ecfdf5;
  box-shadow: 0 14px 30px rgba(15, 118, 110, 0.16);
}

.scan-pop-card.late {
  border-color: #fed7aa;
  border-left-color: #f59e0b;
  background: #fffbeb;
  box-shadow: 0 14px 30px rgba(245, 158, 11, 0.16);
}

.scan-pop-card.excused {
  border-color: #d8e0e5;
  border-left-color: #64748b;
  background: #f8fafc;
  box-shadow: 0 14px 30px rgba(51, 65, 85, 0.12);
}

.scan-pop-card span {
  color: #60717a;
  font-size: 0.78rem;
  font-weight: 900;
  text-transform: uppercase;
}

.scan-pop-card strong {
  min-width: 0;
  color: #142027;
  font-size: 1.25rem;
  line-height: 1.1;
}

.scan-pop-card small {
  min-width: 0;
  color: #394a53;
  font-weight: 800;
}

.scan-pop-card em {
  grid-column: 2;
  grid-row: 1 / span 3;
  color: #60717a;
  font-size: 0.86rem;
  font-style: normal;
  font-weight: 900;
}

.scan-pop-enter-active,
.scan-pop-leave-active {
  transition: opacity 180ms ease, transform 180ms ease;
}

.scan-pop-enter-from,
.scan-pop-leave-to {
  opacity: 0;
  transform: translateY(8px);
}

.badge {
  display: inline-flex;
  min-width: 70px;
  justify-content: center;
  border-radius: 999px;
  padding: 5px 10px;
  background: #fee2e2;
  color: #991b1b;
  font-size: 0.78rem;
  font-weight: 800;
}

.badge.paid {
  background: #dcfce7;
  color: #166534;
}

.badge.neutral {
  background: #e0f2fe;
  color: #075985;
}

.report-panel {
  padding: 20px;
}

.report-list {
  display: grid;
  gap: 12px;
  margin: 0 0 20px;
}

.report-list div,
.mini-stats span {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.report-list dt {
  color: #60717a;
}

.report-list dd {
  margin: 0;
  font-weight: 900;
}

.student-drawer {
  position: fixed;
  right: 24px;
  bottom: 24px;
  width: min(360px, calc(100vw - 48px));
  padding: 20px;
  box-shadow: 0 18px 50px rgba(20, 32, 39, 0.18);
}

.drawer-close {
  float: right;
}

.student-drawer p {
  color: #60717a;
}

.qr-card {
  display: grid;
  justify-items: center;
  gap: 10px;
  margin: 18px 0;
  border: 1px solid #d7e0e4;
  border-radius: 8px;
  padding: 14px;
  background: #ffffff;
}

.qr-card img {
  width: 180px;
  height: 180px;
}

.qr-card figcaption {
  color: #60717a;
  font-size: 0.82rem;
  font-weight: 800;
}

.mini-stats {
  display: grid;
  gap: 10px;
  margin-top: 18px;
}

.mini-stats span {
  border-radius: 8px;
  padding: 12px;
  background: #f4f8f9;
  color: #60717a;
}

.mini-stats strong {
  color: #142027;
}

@media (max-width: 1040px) {
  .workspace,
  .topbar,
  .split-grid,
  .data-layout,
  .qr-scanner-grid,
  .scanner-body,
  .stat-grid {
    grid-template-columns: 1fr;
  }

  .sidebar {
    position: static;
    height: auto;
  }

  .toolbar {
    width: 100%;
    flex-wrap: wrap;
  }

  .search,
  .search input,
  .primary-action,
  .secondary-action {
    width: 100%;
  }

  .fast-scan-strip,
  .compact-grid,
  .compact-grid.three,
  .scan-fallbacks,
  .manual-form {
    grid-template-columns: 1fr;
  }

  .camera-slot,
  .scanner-video {
    min-height: 180px;
  }

  .scan-pop-card {
    grid-template-columns: 1fr;
  }

  .scan-pop-card em {
    grid-column: auto;
    grid-row: auto;
  }

  .absent-fines-browser,
  .absent-event-list article {
    grid-template-columns: 1fr;
  }

  .student-name-list {
    border-right: 0;
    border-bottom: 1px solid #e2e8eb;
  }

  .absent-event-list article div:last-child {
    justify-items: start;
  }
}

@media print {
  .sidebar,
  .toolbar,
  .toast,
  .student-drawer {
    display: none;
  }

  .workspace {
    display: block;
  }

  .content {
    padding: 0;
  }
}
</style>

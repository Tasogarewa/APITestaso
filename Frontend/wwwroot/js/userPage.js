function escapeHtml(str) {
    if (typeof str !== 'string') return String(str || '');
    return str.replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/\\/g, '\\\\');
}

const buttons = document.querySelectorAll('.tab-button');
const panels = document.querySelectorAll('.tab-panel');
const editButton = document.getElementById('edit-profile-button');
const exportButton = document.getElementById('export-results-button');
const profileModal = document.getElementById('edit-profile-modal');
const exportModal = document.getElementById('export-modal');
const cancelProfileButton = document.getElementById('cancel-profile-button');
const saveProfileButton = document.getElementById('save-profile-button');
const cancelExportButton = document.getElementById('cancel-export-button');
const exportConfirmButton = document.getElementById('export-confirm-button');
const profileErrorMessage = document.getElementById('profile-error');
const exportErrorMessage = document.getElementById('export-error');
const emailInput = document.getElementById('email');
const usernameInput = document.getElementById('username');
const phoneInput = document.getElementById('phone');
const connectionStringInput = document.getElementById('connection-string');
const exportFormatSelect = document.getElementById('export-format');

let currentFilters = {
    apis: 'name-asc',
    scenarios: 'name-asc',
    sql: 'name-asc',
    results: 'date-desc'
};

let pieChart, barChart, lineChart;

buttons.forEach(button => {
    button.addEventListener('click', () => {
        buttons.forEach(btn => {
            btn.classList.remove('active');
            btn.setAttribute('aria-selected', 'false');
            btn.setAttribute('tabindex', '-1');
        });
        panels.forEach(panel => panel.classList.remove('active'));

        button.classList.add('active');
        button.setAttribute('aria-selected', 'true');
        button.setAttribute('tabindex', '0');
        const tabId = button.dataset.tab;
        document.getElementById(tabId).classList.add('active');
        if (tabId === 'statistics') {
            loadStatistics();
        } else {
            renderTab(tabId, currentFilters[tabId]);
        }
    });
});

document.querySelectorAll('.filter-button').forEach(button => {
    button.addEventListener('click', () => {
        const tabId = button.closest('.tab-panel').id;
        const filter = button.dataset.filter;
        if (filter === 'reset') {
            currentFilters[tabId] = tabId === 'results' ? 'date-desc' : 'name-asc';
            button.parentElement.querySelectorAll('.filter-button').forEach(btn => {
                btn.classList.toggle('active', btn.dataset.filter === currentFilters[tabId]);
            });
        } else {
            currentFilters[tabId] = filter;
            button.parentElement.querySelectorAll('.filter-button').forEach(btn => btn.classList.remove('active'));
            button.classList.add('active');
        }
        renderTab(tabId, currentFilters[tabId]);
    });
});

function renderTab(tabId, filter) {
    let data, container, html;
    switch (tabId) {
        case 'apis':
            data = filterApis(window.apiTests || [], filter);
            container = document.getElementById('apis-container');
            html = data.map(api => `
                <div class="data-card">
                    <div class="data-card-header">
                        <p>${escapeHtml(api.name || 'Unnamed')}</p>
                        <span class="toggle-icon"></span>
                    </div>
                    <div class="data-card-content">
                        <p>Method: ${escapeHtml(api.method || 'N/A')}</p>
                        <p>URL: ${escapeHtml(api.url || 'N/A')}</p>
                        ${api.expectedResponse ? `<p>Expected Response: ${escapeHtml(api.expectedResponse)}</p>` : ''}
                        <p>Is Mock: ${api.isMock ? 'Yes' : 'No'}</p>
                        ${api.timeoutSeconds ? `<p>Timeout: ${api.timeoutSeconds} seconds</p>` : ''}
                        <p>Expected Status Code: ${escapeHtml(api.expectedStatusCode || 'N/A')}</p>
                        <p>Created By: ${escapeHtml(api.createdByUserId || 'N/A')}</p>
                    </div>
                </div>
            `).join('');
            break;
        case 'scenarios':
            data = filterScenarios(window.scenarios || [], filter);
            container = document.getElementById('scenarios-container');
            html = data.map(scenario => `
                <div class="data-card">
                    <div class="data-card-header">
                        <p>${escapeHtml(scenario.scenarioName || 'Unnamed')}</p>
                        <span class="toggle-icon"></span>
                    </div>
                    <div class="data-card-content">
                        <p>Created By: ${escapeHtml(scenario.createdByUserId || 'N/A')}</p>
                    </div>
                </div>
            `).join('');
            break;
        case 'sql':
            data = filterSqlTests(window.sqlTests || [], filter);
            container = document.getElementById('sql-container');
            html = data.map(sql => `
                <div class="data-card">
                    <div class="data-card-header">
                        <p>${escapeHtml(sql.name || 'Unnamed')}</p>
                        <span class="toggle-icon"></span>
                    </div>
                    <div class="data-card-content">
                        <p>SQL Query: ${escapeHtml(sql.sqlQuery || 'N/A')}</p>
                        <p>Test Type: ${escapeHtml(sql.testType || 'N/A')}</p>
                        ${sql.expectedJson ? `<p>Expected JSON: ${escapeHtml(sql.expectedJson)}</p>` : ''}
                        ${sql.parametersJson ? `<p>Parameters JSON: ${escapeHtml(sql.parametersJson)}</p>` : ''}
                        <p>Database Connection: ${escapeHtml(sql.databaseConnectionName || 'N/A')}</p>
                        <p>Created By: ${escapeHtml(sql.createdByUserId || 'N/A')}</p>
                    </div>
                </div>
            `).join('');
            break;
        case 'results':
            data = filterTestResults(window.testResults || [], filter);
            container = document.getElementById('results-container');
            html = data.map(result => `
                <div class="data-card">
                    <div class="data-card-header">
                        <p>Test ${result.apiTestId ? 'API' : 'SQL'} at ${new Date(result.executedAt || Date.now()).toLocaleString()}</p>
                        <span class="toggle-icon"></span>
                    </div>
                    <div class="data-card-content">
                        <p>Success: ${result.isSuccess ? 'Yes' : 'No'}</p>
                        ${result.response ? `<p>Response: ${escapeHtml(result.response)}</p>` : ''}
                        ${result.errorMessage ? `<p>Error: ${escapeHtml(result.errorMessage)}</p>` : ''}
                        <p>Duration: ${result.durationMilliseconds || 0} ms</p>
                        <p>Executed By: ${escapeHtml(result.executedByUserId || 'N/A')}</p>
                    </div>
                </div>
            `).join('');
            break;
    }

    container.innerHTML = html || `
        <div class="empty-state">
            <img src="https://cdn-icons-png.flaticon.com/512/4712/4712104.png" alt="Empty" />
            <h3>No ${tabId.charAt(0).toUpperCase() + tabId.slice(1)}</h3>
            <p>No matching items found.</p>
        </div>
    `;

    container.querySelectorAll('.data-card').forEach(card => {
        const header = card.querySelector('.data-card-header');
        header.addEventListener('click', () => {
            card.classList.toggle('active');
        });
    });
}
function filterApis(data, filter) {
    let filtered = Array.isArray(data) ? [...data] : [];
    if (filter === 'reset') filter = 'name-asc';
    if (filter === 'name-asc') {
        filtered.sort((a, b) => (a.name || '').localeCompare(b.name || ''));
    } else if (filter === 'name-desc') {
        filtered.sort((a, b) => (b.name || '').localeCompare(a.name || ''));
    } else if (filter === 'method-get') {
        filtered = filtered.filter(api => (api.method || '').toUpperCase() === 'GET');
    } else if (filter === 'method-post') {
        filtered = filtered.filter(api => (api.method || '').toUpperCase() === 'POST');
    } else if (filter === 'is-mock-true') {
        filtered = filtered.filter(api => api.isMock);
    } else if (filter === 'is-mock-false') {
        filtered = filtered.filter(api => !api.isMock);
    }
    return filtered;
}

function filterScenarios(data, filter) {
    let filtered = Array.isArray(data) ? [...data] : [];
    if (filter === 'reset') filter = 'name-asc';
    if (filter === 'name-asc') {
        filtered.sort((a, b) => (a.scenarioName || '').localeCompare(b.scenarioName || ''));
    } else if (filter === 'name-desc') {
        filtered.sort((a, b) => (b.scenarioName || '').localeCompare(a.scenarioName || ''));
    }
    return filtered;
}
function filterSqlTests(data, filter) {
    let filtered = Array.isArray(data) ? [...data] : [];
    if (filter === 'reset') filter = 'name-asc';
    if (filter === 'name-asc') {
        filtered.sort((a, b) => (a.name || '').localeCompare(b.name || ''));
    } else if (filter === 'name-desc') {
        filtered.sort((a, b) => (b.name || '').localeCompare(a.name || ''));
    } else if (filter === 'type-scalar') {
        filtered = filtered.filter(sql => (sql.testType || '') === 0);
    } else if (filter === 'type-resultset') {
        filtered = filtered.filter(sql => (sql.testType || '') === 1);
    } else if (filter === 'type-schema') {
        filtered = filtered.filter(sql => (sql.testType || '') === 2);
    }
    return filtered;
}
function filterTestResults(data, filter) {
    let filtered = Array.isArray(data) ? [...data] : [];
    if (filter === 'reset') filter = 'date-desc';
    if (filter === 'date-desc') {
        filtered.sort((a, b) => new Date(b.executedAt || Date.now()) - new Date(a.executedAt || Date.now()));
    } else if (filter === 'date-asc') {
        filtered.sort((a, b) => new Date(a.executedAt || Date.now()) - new Date(b.executedAt || Date.now()));
    } else if (filter === 'success-true') {
        filtered = filtered.filter(result => result.isSuccess);
    } else if (filter === 'success-false') {
        filtered = filtered.filter(result => !result.isSuccess);
    }
    return filtered;
}
// Initial render
if (window.apiTests?.length > 0) renderTab('apis', 'name-asc');
if (window.scenarios?.length > 0) renderTab('scenarios', 'name-asc');
if (window.sqlTests?.length > 0) renderTab('sql', 'name-asc');
if (window.testResults?.length > 0) renderTab('results', 'date-desc');
// Export functionality
if (exportButton) {
    exportButton.addEventListener('click', () => {
        exportModal.classList.add('active');
        exportErrorMessage.classList.remove('active');
    });
}

if (cancelExportButton) {
    cancelExportButton.addEventListener('click', () => {
        exportModal.classList.remove('active');
        exportFormatSelect.value = '';
    });
}

if (exportConfirmButton) {
    exportConfirmButton.addEventListener('click', () => {
        const format = exportFormatSelect.value;
        if (!format) {
            exportErrorMessage.textContent = 'Please select a format.';
            exportErrorMessage.classList.add('active');
            return;
        }

        const filteredResults = filterTestResults(testResults, currentFilters.results);
        let content, filename, mimeType;

        switch (format) {
            case 'json':
                content = JSON.stringify(filteredResults, null, 2);
                filename = 'test_results.json';
                mimeType = 'application/json';
                break;
            case 'xml':
                content = generateXml(filteredResults);
                filename = 'test_results.xml';
                mimeType = 'application/xml';
                break;
            case 'csv':
                content = generateCsv(filteredResults);
                filename = 'test_results.csv';
                mimeType = 'text/csv';
                break;
        }

        downloadFile(content, filename, mimeType);
        exportModal.classList.remove('active');
        exportFormatSelect.value = '';
    });
}

function generateXml(data) {
    let xml = '<?xml version="1.0" encoding="UTF-8"?>\n<TestResults>\n';
    (data || []).forEach(result => {
        xml += `  <TestResult>\n`;
        xml += `    <ExecutedAt>${new Date(result.executedAt || Date.now()).toISOString()}</ExecutedAt>\n`;
        xml += `    <IsSuccess>${result.isSuccess}</IsSuccess>\n`;
        xml += `    <Response>${escapeHtml(result.response || '')}</Response>\n`;
        xml += `    <ErrorMessage>${escapeHtml(result.errorMessage || '')}</ErrorMessage>\n`;
        xml += `    <DurationMilliseconds>${result.durationMilliseconds || 0}</DurationMilliseconds>\n`;
        xml += `    <ExecutedByUserId>${escapeHtml(result.executedByUserId || 'N/A')}</ExecutedByUserId>\n`;
        xml += `    <TestType>${result.apiTestId ? 'API' : 'SQL'}</TestType>\n`;
        xml += `  </TestResult>\n`;
    });
    xml += '</TestResults>';
    return xml;
}

function generateCsv(data) {
    const headers = ['ExecutedAt', 'IsSuccess', 'Response', 'ErrorMessage', 'DurationMilliseconds', 'ExecutedByUserId', 'TestType'];
    let csv = headers.join(',') + '\n';
    (data || []).forEach(result => {
        const row = [
            `"${new Date(result.executedAt || Date.now()).toLocaleString()}"`,
            result.isSuccess,
            `"${(result.response || '').replace(/"/g, '""')}"`,
            `"${(result.errorMessage || '').replace(/"/g, '""')}"`,
            result.durationMilliseconds || 0,
            `"${result.executedByUserId || 'N/A'}"`,
            result.apiTestId ? 'API' : 'SQL'
        ];
        csv += row.join(',') + '\n';
    });
    return csv;
}

function downloadFile(content, filename, mimeType) {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

// Statistics functionality
async function loadStatistics() {
    try {
        console.log('Fetching statistics for user:', window.currentUserId);
        const response = await fetch(`https://localhost:7200/api/TestStatistics/user/${window.currentUserId}`, {
            headers: {
                'Authorization': `Bearer ${getJwtToken()}`
            },
            credentials: 'include' // Додаємо cookie
        });
        if (response.ok) {
            const stats = await response.json();
            console.log('Statistics:', stats);
            renderCharts(stats);
        } else {
            console.error('Failed to load statistics:', response.status, response.statusText);
            document.getElementById('statistics-container').innerHTML = `
                <div class="empty-state">
                    <img src="https://cdn-icons-png.flaticon.com/512/4193/419.png" alt="Empty" />
                    <h3>No Statistics</h3>
                    <p>Unable to load statistics: ${response.status} ${response.statusText}</p>
                </div>
            `;
        }
    } catch (err) {
        console.error('Error fetching statistics:', err);
        document.getElementById('statistics-container').innerHTML = `
            <div class="empty-state">
                <img src="https://cdn-icons-png.flaticon.com/512/4193/419.png" alt="Empty" />
                <h3>Error</h3>
                <p>Error fetching statistics: ${err.message}</p>
            </div>
        `;
    }
}

if (editButton) {
    editButton.addEventListener('click', async () => {
        try {
            console.log('Fetching user profile');
            const response = await fetch('https://localhost:7200/api/account/me', {
                headers: {
                    'Authorization': `Bearer ${getJwtToken()}`
                },
                credentials: 'include' // Додаємо cookie
            });
            if (response.ok) {
                const user = await response.json();
                emailInput.value = user?.email || '';
                usernameInput.value = user?.userName || '';
                phoneInput.value = user?.phoneNumber || '';
                connectionStringInput.value = user?.databaseConnectionString || '';
                profileModal.classList.add('active');
                profileErrorMessage.classList.remove('active');
            } else {
                console.error('Failed to load user data:', response.status, response.statusText);
                showProfileError(`Failed to load user data: ${response.status} ${response.statusText}`);
            }
        } catch (err) {
            console.error('Error fetching user data:', err);
            showProfileError('Error fetching user data.');
        }
    });
}

function renderCharts(stats) {
    const ctxPie = document.getElementById('pie-chart').getContext('2d');
    const ctxBar = document.getElementById('bar-chart').getContext('2d');
    const ctxLine = document.getElementById('line-chart').getContext('2d');

    // Destroy existing charts if they exist
    if (pieChart) pieChart.destroy();
    if (barChart) barChart.destroy();
    if (lineChart) lineChart.destroy();

    // Pie Chart: Total Success vs Failed
    pieChart = new Chart(ctxPie, {
        type: 'pie',
        data: {
            labels: ['Success', 'Failed'],
            datasets: [{
                data: [stats?.totalSuccess || 0, stats?.totalFailed || 0],
                backgroundColor: ['#8aa8c3', '#ff6b6b'],
                borderColor: ['#1c2526', '#1c2526'],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    labels: {
                        color: '#8aa8c3',
                        font: { size: 12 }
                    }
                }
            }
        }
    });

    // Bar Chart: Success/Failed by Test Type
    const types = Object.keys(stats?.resultsByType || {});
    barChart = new Chart(ctxBar, {
        type: 'bar',
        data: {
            labels: types,
            datasets: [
                {
                    label: 'Success',
                    data: types.map(type => stats.resultsByType[type]?.success || 0),
                    backgroundColor: '#8aa8c3',
                    borderColor: '#1c2526',
                    borderWidth: 1
                },
                {
                    label: 'Failed',
                    data: types.map(type => stats.resultsByType[type]?.failed || 0),
                    backgroundColor: '#ff6b6b',
                    borderColor: '#1c2526',
                    borderWidth: 1
                }
            ]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    labels: {
                        color: '#8aa8c3',
                        font: { size: 12 }
                    }
                }
            },
            scales: {
                x: { ticks: { color: '#8aa8c3' } },
                y: { ticks: { color: '#8aa8c3' } }
            }
        }
    });

    // Line Chart: Execution Duration Trend
    lineChart = new Chart(ctxLine, {
        type: 'line',
        data: {
            labels: (stats?.executionTrend || []).map(point => point?.label || 'N/A'),
            datasets: [{
                label: 'Duration (ms)',
                data: (stats?.executionTrend || []).map(point => point?.duration || 0),
                borderColor: '#8aa8c3',
                backgroundColor: 'rgba(138, 168, 195, 0.2)',
                fill: true,
                tension: 0.4
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    labels: {
                        color: '#8aa8c3',
                        font: { size: 12 }
                    }
                }
            },
            scales: {
                x: { ticks: { color: '#8aa8c3' } },
                y: { ticks: { color: '#8aa8c3' } }
            }
        }
    });
}

// Modal functionality
if (editButton) {
    editButton.addEventListener('click', async () => {
        try {
            console.log('Fetching user profile');
            const response = await fetch('https://localhost:7200/api/account/me', {
                headers: {
                    'Authorization': `Bearer ${getJwtToken()}`
                }
            });
            if (response.ok) {
                const user = await response.json();
                emailInput.value = user?.email || '';
                usernameInput.value = user?.userName || '';
                phoneInput.value = user?.phoneNumber || '';
                connectionStringInput.value = user?.databaseConnectionString || '';
                profileModal.classList.add('active');
                profileErrorMessage.classList.remove('active');
            } else {
                console.error('Failed to load user data:', response.status, response.statusText);
                showProfileError(`Failed to load user data: ${response.status} ${response.statusText}`);
            }
        } catch (err) {
            console.error('Error fetching user data:', err);
            showProfileError('Error fetching user data.');
        }
    });
}

if (cancelProfileButton) {
    cancelProfileButton.addEventListener('click', () => {
        profileModal.classList.remove('active');
        clearProfileForm();
    });
}

if (saveProfileButton) {
    saveProfileButton.addEventListener('click', async () => {
        if (!emailInput.value || !usernameInput.value) {
            showProfileError('Email and Full Name are required.');
            return;
        }
        if (!/\S+@\S+\.\S+/.test(emailInput.value)) {
            showProfileError('Invalid email format.');
            return;
        }

        const profileData = {
            email: emailInput.value,
            userName: usernameInput.value,
            phoneNumber: phoneInput.value
        };

        const connectionString = connectionStringInput.value;

        try {
            const profileResponse = await fetch('https://localhost:7200/api/account/UpdateProfile', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${getJwtToken()}`
                },
                body: JSON.stringify(profileData)
            });

            if (!profileResponse.ok) {
                const errors = await profileResponse.json();
                showProfileError(errors?.map(e => e.description).join(', ') || 'Failed to update profile.');
                return;
            }

            const connectionResponse = await fetch('https://localhost:7200/api/account/SetConnectionString', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${getJwtToken()}`
                },
                body: JSON.stringify(connectionString)
            });

            if (!connectionResponse.ok) {
                const error = await connectionResponse.text();
                showProfileError(error || 'Failed to set connection string.');
                return;
            }

            document.querySelector('.profile-email').textContent = emailInput.value;
            profileModal.classList.remove('active');
            clearProfileForm();
        } catch (err) {
            showProfileError('Error saving profile.');
        }
    });
}

function getJwtToken() {
    const cookies = document.cookie.split(';');
    for (let cookie of cookies) {
        const [name, value] = cookie.trim().split('=');
        if (name === 'AccessToken') return value || '';
    }
    return '';
}

function showProfileError(message) {
    profileErrorMessage.textContent = message;
    profileErrorMessage.classList.add('active');
}
function clearProfileForm() {
    emailInput.value = '';
    usernameInput.value = '';
    phoneInput.value = '';
    connectionStringInput.value = '';
    profileErrorMessage.classList.remove('active');
}


document.addEventListener('DOMContentLoaded', () => {
    
    const activeTab = document.querySelector('.tab-button.active');
    if (activeTab) {
        const tabId = activeTab.dataset.tab;
        if (tabId !== 'statistics') {
            renderTab(tabId, currentFilters[tabId]);
        }
    }
});
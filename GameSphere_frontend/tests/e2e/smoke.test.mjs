import assert from 'node:assert/strict';
import { test } from 'node:test';
import { chromium } from 'playwright';

const baseUrl = process.env.E2E_BASE_URL ?? 'http://localhost:8080';
const adminEmail = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;

async function expectJson(response, expectedStatus) {
  const body = await response.text();
  assert.equal(response.status(), expectedStatus, body);
  return body ? JSON.parse(body) : null;
}

test('Docker stack serves the frontend and protects the API', async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();

  try {
    const page = await context.newPage();
    const frontendResponse = await page.goto(`${baseUrl}/`, { waitUntil: 'domcontentloaded' });
    assert.equal(frontendResponse?.status(), 200);
    assert.equal(await page.locator('html').count(), 1);

    const protectedResponse = await context.request.get(`${baseUrl}/api/quizzes`);
    assert.equal(protectedResponse.status(), 401);

    const foreignOriginResponse = await context.request.post(`${baseUrl}/api/User/login`, {
      headers: { Origin: 'https://attacker.example' },
      data: { email: 'unknown@example.test', password: 'irrelevant-password' },
    });
    assert.equal(foreignOriginResponse.status(), 403);

    const headers = frontendResponse.headers();
    assert.equal(headers['x-content-type-options'], 'nosniff');
    assert.equal(headers['x-frame-options'], 'DENY');
    assert.match(headers['content-security-policy'], /frame-ancestors 'self'/);
  } finally {
    await context.close();
    await browser.close();
  }
});

test('registered user can log in and complete a published quiz', {
  skip: !adminEmail || !adminPassword,
}, async () => {
  const browser = await chromium.launch({ headless: true });
  const adminContext = await browser.newContext();
  const userContext = await browser.newContext();
  const adminRequest = adminContext.request;
  const userRequest = userContext.request;
  const suffix = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  const userEmail = `e2e-${suffix}@example.test`;
  const quizTitle = `E2E Quiz ${suffix}`;
  let quizId;
  let userId;

  try {
    const adminLogin = await adminRequest.post(`${baseUrl}/api/User/login`, {
      data: { email: adminEmail, password: adminPassword },
    });
    await expectJson(adminLogin, 200);

    const quizResponse = await adminRequest.post(`${baseUrl}/api/admin/quizzes`, {
      data: {
        title: quizTitle,
        difficulty: 0,
        numberOfQuests: 1,
        isPublished: true,
      },
    });
    const quiz = await expectJson(quizResponse, 201);
    quizId = quiz.id;

    const questionResponse = await adminRequest.post(`${baseUrl}/api/admin/quizzes/${quizId}/questions`, {
      data: {
        description: 'Which answer is correct?',
        typeOfAnswer: 0,
        answers: ['Correct answer', 'Wrong answer'],
        correctAnswer: 'Correct answer',
        quizzId: quizId,
      },
    });
    await expectJson(questionResponse, 201);

    const registrationResponse = await userRequest.post(`${baseUrl}/api/User`, {
      data: {
        firstName: 'E2E',
        lastName: 'Player',
        email: userEmail,
        password: 'E2e-password-123',
        gender: 2,
      },
    });
    const registeredUser = await expectJson(registrationResponse, 201);
    userId = registeredUser.id;

    const page = await userContext.newPage();
    await page.goto(`${baseUrl}/login`, { waitUntil: 'domcontentloaded' });
    await page.getByPlaceholder('Enter your email').fill(userEmail);
    await page.getByPlaceholder('Enter your password').fill('E2e-password-123');
    await page.getByRole('button', { name: 'Log In', exact: true }).click();
    await page.waitForURL('**/profile');

    await page.goto(`${baseUrl}/quizzes`, { waitUntil: 'networkidle' });
    await page.getByRole('link', { name: new RegExp(quizTitle) }).click();
    await page.getByRole('radio', { name: 'Correct answer' }).check();
    await page.getByRole('button', { name: 'Terminar quiz' }).click();
    await page.getByText('Tentativa concluída').waitFor();
    await page.getByText('100%').waitFor();
  } finally {
    if (userId) {
      await userRequest.delete(`${baseUrl}/api/User/${userId}`).catch(() => undefined);
    }
    if (quizId) {
      await adminRequest.delete(`${baseUrl}/api/admin/quizzes/${quizId}`).catch(() => undefined);
    }
    await adminContext.close();
    await userContext.close();
    await browser.close();
  }
});

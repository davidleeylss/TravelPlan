/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./index.html",
        "./src/**/*.{vue,js,ts,jsx,tsx}",
    ],
    theme: {
        extend: {
            colors: {
                //'primary': '#3b82f6',
                'primary': '#0EA5E9',
                'lake-dark': '#0ea5e9',
                'soft-gray': '#F5F7FA',
            },
            fontFamily: {
                sans: ['"Noto Sans JP"', 'sans-serif'],
            }
        },
    },
    plugins: [],
}

